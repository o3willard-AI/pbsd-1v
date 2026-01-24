using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PairAdmin.LLMGateway.Models;

namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// Google Gemini provider implementation of ILLMProvider
/// </summary>
public class GeminiProvider : ILLMProvider
{
    private readonly ILogger<GeminiProvider> _logger;
    private readonly HttpClient _httpClient;
    private ProviderConfiguration? _configuration;

    private static readonly List<string> SupportedModels = new()
    {
        "gemini-pro",
        "gemini-1.5-pro",
        "gemini-1.5-flash"
    };

    private static readonly Provider _providerInfo = new()
    {
        Id = "gemini",
        Name = "Google Gemini",
        Description = "Google Gemini AI models",
        SupportedModels = SupportedModels,
        Endpoint = "https://generativelanguage.googleapis.com/v1",
        SupportsStreaming = true,
        MaxContextTokens = 1000000,
        DefaultModel = "gemini-pro"
    };

    public bool IsConfigured => _configuration != null && !string.IsNullOrWhiteSpace(_configuration.ApiKey);

    public GeminiProvider(ILogger<GeminiProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _logger.LogInformation("GeminiProvider initialized");
    }

    public Provider GetProviderInfo() => _providerInfo;

    public void Configure(ProviderConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new LLMGatewayException("API key is required for Gemini provider");

        _configuration = configuration;
        var baseUrl = configuration.BaseUrl ?? "https://generativelanguage.googleapis.com/v1";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", configuration.ApiKey);

        _logger.LogInformation($"GeminiProvider configured with model: {configuration.Model}");
    }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
            throw new LLMGatewayException("Provider is not configured");

        try
        {
            var geminiRequest = ConvertToGeminiRequest(request);
            var json = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{request.Model}:generateContent",
                content, cancellationToken);

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            if (geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
            {
                throw new LLMProviderException(_providerInfo.Id, "No content in response");
            }

            var result = new CompletionResponse
            {
                Content = geminiResponse.Candidates.First().Content.Parts.First().Text,
                Provider = _providerInfo.Id,
                Model = request.Model,
                Timestamp = DateTime.Now
            };

            _logger.LogInformation("Gemini completion received");
            return result;
        }
        catch (LLMProviderException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Gemini request");
            throw new LLMProviderException(_providerInfo.Id, "Failed to complete request", ex);
        }
    }

    public async IAsyncEnumerable<StreamingCompletionResponse> StreamCompleteAsync(
        CompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_configuration == null)
            throw new LLMGatewayException("Provider is not configured");

        var responses = new List<StreamingCompletionResponse>();
        Exception? capturedException = null;

        try
        {
            var geminiRequest = ConvertToGeminiRequest(request);
            var json = JsonSerializer.Serialize(geminiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"models/{request.Model}:streamGenerateContent?alt=sse",
                content, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            var cumulativeContent = new StringBuilder();
            var chunkIndex = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line["data: ".Length..];
                    if (data == "[DONE]")
                    {
                        responses.Add(StreamingCompletionResponse.Complete("stop"));
                        break;
                    }

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<GeminiResponse>(data);
                        var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            cumulativeContent.Append(text);
                            responses.Add(new StreamingCompletionResponse(text, chunkIndex++)
                            {
                                Provider = _providerInfo.Id,
                                Model = request.Model,
                                CumulativeTokens = EstimateTokenCount(cumulativeContent.ToString())
                            });
                        }
                    }
                    catch { /* Skip invalid chunks */ }
                }
            }
        }
        catch (Exception ex)
        {
            capturedException = ex;
        }

        foreach (var response in responses)
        {
            yield return response;
        }

        if (capturedException != null)
        {
            throw new LLMProviderException(_providerInfo.Id, "Streaming failed", capturedException);
        }
    }

    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Length / 4;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"models/{_configuration?.Model ?? "gemini-pro"}?key={_configuration?.ApiKey}",
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini connection test failed");
            return false;
        }
    }

    public bool ValidateConfiguration(ProviderConfiguration configuration, out string? errorMessage)
    {
        errorMessage = null;
        if (configuration == null)
        {
            errorMessage = "Configuration is null";
            return false;
        }
        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            errorMessage = "API key is required for Gemini";
            return false;
        }
        return true;
    }

    public List<string> GetSupportedModels() => new List<string>(SupportedModels);
    public string GetDefaultModel() => _providerInfo.DefaultModel;
    public bool SupportsStreaming() => true;
    public int GetMaxContext() => _providerInfo.MaxContextTokens;

    private GeminiRequest ConvertToGeminiRequest(CompletionRequest request)
    {
        return new GeminiRequest
        {
            Contents = request.Messages.Select(m => new GeminiContent
            {
                Role = m.RoleString == "user" ? "user" : "model",
                Parts = new List< GeminiPart> { new GeminiPart { Text = m.Content } }
            }).ToList(),
            GenerationConfig = new GeminiConfig
            {
                Temperature = request.Temperature,
                MaxOutputTokens = request.MaxTokens,
                TopP = request.TopP
            }
        };
    }

    private class GeminiRequest
    {
        public List<GeminiContent>? Contents { get; set; }
        public GeminiConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        public string? Role { get; set; }
        public List<GeminiPart>? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class GeminiConfig
    {
        public double Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
        public double TopP { get; set; }
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }
}
