using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PairAdmin.LLMGateway.Models;

namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// OpenRouter provider implementation of ILLMProvider
/// </summary>
public class OpenRouterProvider : ILLMProvider
{
    private readonly ILogger<OpenRouterProvider> _logger;
    private readonly HttpClient _httpClient;
    private ProviderConfiguration? _configuration;

    private static readonly List<string> SupportedModels = new()
    {
        "openrouter/auto",
        "anthropic/claude-3-opus",
        "anthropic/claude-3-sonnet",
        "anthropic/claude-3-haiku",
        "openai/gpt-4",
        "openai/gpt-4-turbo",
        "openai/gpt-3.5-turbo",
        "google/gemini-pro",
        "meta-llama/llama-3-70b-instruct",
        "mistral/mistral-large"
    };

    private static readonly Provider _providerInfo = new()
    {
        Id = "openrouter",
        Name = "OpenRouter",
        Description = "Unified API for multiple LLM providers",
        SupportedModels = SupportedModels,
        Endpoint = "https://openrouter.ai/api/v1",
        SupportsStreaming = true,
        MaxContextTokens = 128000,
        DefaultModel = "openrouter/auto"
    };

    public bool IsConfigured => _configuration != null && !string.IsNullOrWhiteSpace(_configuration.ApiKey);

    public OpenRouterProvider(ILogger<OpenRouterProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _logger.LogInformation("OpenRouterProvider initialized");
    }

    public Provider GetProviderInfo() => _providerInfo;

    public void Configure(ProviderConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            throw new LLMGatewayException("API key is required for OpenRouter provider");

        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(configuration.BaseUrl ?? "https://openrouter.ai/api/v1");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://pairadmin.app");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "PairAdmin");

        if (configuration.OrganizationId != null)
            _httpClient.DefaultRequestHeaders.Add("OpenRouter-Organization", configuration.OrganizationId);

        _logger.LogInformation($"OpenRouterProvider configured with model: {configuration.Model}");
    }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
            throw new LLMGatewayException("Provider is not configured");

        try
        {
            var openRouterRequest = new OpenRouterRequest
            {
                Model = request.Model,
                Messages = request.Messages.Select(m => new OpenRouterMessage
                {
                    Role = m.RoleString,
                    Content = m.Content
                }).ToList(),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopP = request.TopP,
                Stream = false
            };

            var json = JsonSerializer.Serialize(openRouterRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson);

            var choice = openRouterResponse?.Choices?.FirstOrDefault();
            if (choice?.Message?.Content == null)
                throw new LLMProviderException(_providerInfo.Id, "No content in response");

            var result = new CompletionResponse
            {
                Content = choice.Message.Content,
                Provider = _providerInfo.Id,
                Model = openRouterResponse.Model,
                Timestamp = DateTime.Now
            };

            _logger.LogInformation("OpenRouter completion received");
            return result;
        }
        catch (LLMProviderException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete OpenRouter request");
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
            var openRouterRequest = new OpenRouterRequest
            {
                Model = request.Model,
                Messages = request.Messages.Select(m => new OpenRouterMessage
                {
                    Role = m.RoleString,
                    Content = m.Content
                }).ToList(),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopP = request.TopP,
                Stream = true
            };

            var json = JsonSerializer.Serialize(openRouterRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
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
                        var chunk = JsonSerializer.Deserialize<OpenRouterChunk>(data);
                        var delta = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                        
                        if (!string.IsNullOrEmpty(delta))
                        {
                            cumulativeContent.Append(delta);
                            responses.Add(new StreamingCompletionResponse(delta, chunkIndex++)
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
            var response = await _httpClient.GetAsync("/models", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenRouter connection test failed");
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
            errorMessage = "API key is required for OpenRouter";
            return false;
        }
        return true;
    }

    public List<string> GetSupportedModels() => new List<string>(SupportedModels);
    public string GetDefaultModel() => _providerInfo.DefaultModel;
    public bool SupportsStreaming() => true;
    public int GetMaxContext() => _providerInfo.MaxContextTokens;

    private class OpenRouterRequest
    {
        public string Model { get; set; } = "";
        public List<OpenRouterMessage>? Messages { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public double TopP { get; set; }
        public bool Stream { get; set; }
    }

    private class OpenRouterMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private class OpenRouterResponse
    {
        public string? Model { get; set; }
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    private class OpenRouterChoice
    {
        public OpenRouterMessage? Message { get; set; }
        public OpenRouterDelta? Delta { get; set; }
    }

    private class OpenRouterDelta
    {
        public string? Content { get; set; }
    }

    private class OpenRouterChunk
    {
        public List<OpenRouterChoice>? Choices { get; set; }
    }
}
