using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PairAdmin.LLMGateway.Models;

namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// Ollama local provider implementation of ILLMProvider
/// </summary>
public class OllamaProvider : ILLMProvider
{
    private readonly ILogger<OllamaProvider> _logger;
    private readonly HttpClient _httpClient;
    private ProviderConfiguration? _configuration;

    private static readonly List<string> SupportedModels = new()
    {
        "llama3",
        "llama3.1",
        "mistral",
        "mixtral",
        "codellama",
        "phi3",
        "gemma",
        "gemma2",
        "llava",
        "neural-chat"
    };

    private static readonly Provider _providerInfo = new()
    {
        Id = "ollama",
        Name = "Ollama (Local)",
        Description = "Run LLMs locally with Ollama",
        SupportedModels = SupportedModels,
        Endpoint = "http://localhost:11434",
        SupportsStreaming = true,
        MaxContextTokens = 8192,
        DefaultModel = "llama3"
    };

    public bool IsConfigured => _configuration != null;

    public OllamaProvider(ILogger<OllamaProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        _logger.LogInformation("OllamaProvider initialized");
    }

    public Provider GetProviderInfo() => _providerInfo;

    public void Configure(ProviderConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(configuration.BaseUrl ?? "http://localhost:11434");

        _logger.LogInformation($"OllamaProvider configured for {configuration.BaseUrl} with model: {configuration.Model}");
    }

    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
            throw new LLMGatewayException("Provider is not configured");

        try
        {
            var ollamaRequest = new OllamaRequest
            {
                Model = request.Model,
                Prompt = request.Messages.LastOrDefault()?.Content ?? "",
                System = request.Messages.FirstOrDefault(m => m.RoleString == "system")?.Content,
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = request.Temperature,
                    NumPredict = request.MaxTokens,
                    TopK = (int)(request.TopP * 100)
                }
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (string.IsNullOrEmpty(ollamaResponse?.Response))
                throw new LLMProviderException(_providerInfo.Id, "No content in response");

            var result = new CompletionResponse
            {
                Content = ollamaResponse.Response,
                Provider = _providerInfo.Id,
                Model = request.Model,
                Timestamp = DateTime.Now
            };

            _logger.LogInformation("Ollama completion received");
            return result;
        }
        catch (LLMProviderException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Ollama request");
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
            var ollamaRequest = new OllamaRequest
            {
                Model = request.Model,
                Prompt = request.Messages.LastOrDefault()?.Content ?? "",
                System = request.Messages.FirstOrDefault(m => m.RoleString == "system")?.Content,
                Stream = true,
                Options = new OllamaOptions
                {
                    Temperature = request.Temperature,
                    NumPredict = request.MaxTokens,
                    TopK = (int)(request.TopP * 100)
                }
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            var cumulativeContent = new StringBuilder();
            var chunkIndex = 0;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                try
                {
                    var chunk = JsonSerializer.Deserialize<OllamaChunk>(line);
                    
                    if (!string.IsNullOrEmpty(chunk?.Response))
                    {
                        cumulativeContent.Append(chunk.Response);
                        
                        responses.Add(new StreamingCompletionResponse(chunk.Response, chunkIndex++)
                        {
                            Provider = _providerInfo.Id,
                            Model = request.Model,
                            CumulativeTokens = EstimateTokenCount(cumulativeContent.ToString()),
                            IsComplete = chunk.Done
                        });

                        if (chunk.Done)
                        {
                            responses.Add(StreamingCompletionResponse.Complete(chunk.DoneReason ?? "stop"));
                        }
                    }
                }
                catch { /* Skip invalid chunks */ }
            }
        }
        catch (Exception ex)
        {
            capturedException = ex;
        }

        foreach (var resp in responses)
        {
            yield return resp;
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
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama connection test failed - make sure Ollama is running");
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
        if (string.IsNullOrWhiteSpace(configuration.BaseUrl))
        {
            errorMessage = "BaseUrl is required for Ollama (e.g., http://localhost:11434)";
            return false;
        }
        return true;
    }

    public List<string> GetSupportedModels() => new List<string>(SupportedModels);
    public string GetDefaultModel() => _providerInfo.DefaultModel;
    public bool SupportsStreaming() => true;
    public int GetMaxContext() => _configuration?.MaxContext ?? _providerInfo.MaxContextTokens;

    private class OllamaRequest
    {
        public string Model { get; set; } = "";
        public string? Prompt { get; set; }
        public string? System { get; set; }
        public bool Stream { get; set; }
        public OllamaOptions? Options { get; set; }
    }

    private class OllamaOptions
    {
        public double Temperature { get; set; }
        public int NumPredict { get; set; }
        public int TopK { get; set; }
    }

    private class OllamaResponse
    {
        public string? Response { get; set; }
    }

    private class OllamaChunk
    {
        public string? Response { get; set; }
        public bool Done { get; set; }
        public string? DoneReason { get; set; }
    }
}
