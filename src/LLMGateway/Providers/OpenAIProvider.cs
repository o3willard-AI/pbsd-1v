using Microsoft.Extensions.Logging;
using PairAdmin.LLMGateway.Models;

namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// OpenAI provider implementation of ILLMProvider
/// </summary>
public class OpenAIProvider : ILLMProvider
{
    private readonly OpenAIClient _client;
    private readonly ILogger<OpenAIProvider> _logger;
    private ProviderConfiguration? _configuration;

    /// <summary>
    /// Supported OpenAI models
    /// </summary>
    private static readonly List<string> SupportedModels = new()
    {
        "gpt-3.5-turbo",
        "gpt-4",
        "gpt-4-turbo",
        "gpt-4-turbo-preview"
    };

    /// <summary>
    /// Provider information
    /// </summary>
    private static readonly Provider _providerInfo = new()
    {
        Id = "openai",
        Name = "OpenAI",
        Description = "OpenAI GPT models",
        SupportedModels = SupportedModels,
        Endpoint = "https://api.openai.com/v1",
        SupportsStreaming = true,
        MaxContextTokens = 128000,
        DefaultModel = "gpt-3.5-turbo"
    };

    /// <summary>
    /// Gets provider information
    /// </summary>
    public bool IsConfigured => _configuration != null && !string.IsNullOrWhiteSpace(_configuration.ApiKey);

    /// <summary>
    /// Initializes a new instance of OpenAIProvider
    /// </summary>
    public OpenAIProvider(ILogger<OpenAIProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new OpenAIClient(_logger);
        _logger.LogInformation("OpenAIProvider initialized");
    }

    /// <summary>
    /// Gets provider information
    /// </summary>
    public Provider GetProviderInfo()
    {
        return _providerInfo;
    }

    /// <summary>
    /// Configures the provider
    /// </summary>
    public void Configure(ProviderConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(configuration.ApiKey))
        {
            throw new LLMGatewayException("API key is required for OpenAI provider");
        }

        _configuration = configuration;

        var baseUrl = configuration.BaseUrl ?? "https://api.openai.com";
        _client.Configure(
            configuration.ApiKey,
            configuration.OrganizationId,
            baseUrl);

        _logger.LogInformation($"OpenAIProvider configured with model: {configuration.Model}");
    }

    /// <summary>
    /// Sends a completion request
    /// </summary>
    public async Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
        {
            throw new LLMGatewayException("Provider is not configured");
        }

        try
        {
            var openaiRequest = ConvertToOpenAIRequest(request);
            var openaiResponse = await _client.SendRequestAsync(openaiRequest);

            if (openaiResponse == null)
            {
                throw new LLMProviderException(
                    _providerInfo.Id,
                    "Received null response from OpenAI");
            }

            var response = ConvertFromOpenAIResponse(openaiResponse);
            _logger.LogInformation($"Completion received. Tokens: {response.TotalTokens}");

            return response;
        }
        catch (LLMProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete request");
            throw new LLMProviderException(
                _providerInfo.Id,
                "Failed to complete request",
                ex);
        }
    }

    /// <summary>
    /// Sends a streaming completion request
    /// </summary>
    public async IAsyncEnumerable<StreamingCompletionResponse> StreamCompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_configuration == null)
        {
            throw new LLMGatewayException("Provider is not configured");
        }

        try
        {
            var openaiRequest = ConvertToOpenAIRequest(request);
            var chunkIndex = 0;
            var cumulativeContent = new StringBuilder();

            await foreach (var chunk in _client.SendStreamingRequestAsync(openaiRequest))
            {
                if (chunk.IsDone)
                {
                    yield return StreamingCompletionResponse.Complete(chunk.Choices?.FirstOrDefault()?.FinishReason ?? "stop");
                    break;
                }

                var content = ExtractDeltaContent(chunk);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    cumulativeContent.Append(content);

                    yield return new StreamingCompletionResponse(content, chunkIndex)
                    {
                        Provider = _providerInfo.Id,
                        Model = chunk.Model,
                        CumulativeTokens = EstimateTokenCount(cumulativeContent.ToString())
                    };

                    chunkIndex++;
                }
            }

            _logger.LogInformation($"Streaming completed. Total chunks: {chunkIndex}");
        }
        catch (LLMProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream completion");
            throw new LLMProviderException(
                _providerInfo.Id,
                "Failed to stream completion",
                ex);
        }
    }

    /// <summary>
    /// Estimates token count for text
    /// </summary>
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Length / 4;
    }

    /// <summary>
    /// Tests provider connection
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Validates configuration
    /// </summary>
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
            errorMessage = "API key is required for OpenAI provider";
            return false;
        }

        if (!SupportedModels.Contains(configuration.Model, StringComparer.OrdinalIgnoreCase))
        {
            errorMessage = $"Model '{configuration.Model}' is not supported. " +
                           $"Supported models: {string.Join(", ", SupportedModels)}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets supported models for this provider
    /// </summary>
    public List<string> GetSupportedModels()
    {
        return new List<string>(SupportedModels);
    }

    /// <summary>
    /// Gets default model for this provider
    /// </summary>
    public string GetDefaultModel()
    {
        return _providerInfo.DefaultModel;
    }

    /// <summary>
    /// Gets whether provider supports streaming
    /// </summary>
    public bool SupportsStreaming()
    {
        return _providerInfo.SupportsStreaming;
    }

    /// <summary>
    /// Gets the maximum context window for the current configuration
    /// </summary>
    public int GetMaxContext()
    {
        var modelContext = _providerInfo.MaxContextTokens;

        if (_configuration?.MaxContext > 0)
        {
            modelContext = Math.Min(modelContext, _configuration.MaxContext);
        }

        return modelContext;
    }

    private OpenAICompletionRequest ConvertToOpenAIRequest(CompletionRequest request)
    {
        var messages = request.Messages.Select(m => new OpenAIMessage
        {
            Role = m.RoleString,
            Content = m.Content
        }).ToList();

        return new OpenAICompletionRequest
        {
            Model = request.Model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            TopP = request.TopP,
            Stream = request.Stream,
            N = request.N,
            PresencePenalty = request.PresencePenalty,
            FrequencyPenalty = request.FrequencyPenalty,
            Stop = request.Stop
        };
    }

    private CompletionResponse ConvertFromOpenAIResponse(OpenAICompletionResponse openaiResponse)
    {
        var choice = openaiResponse.Choices?.FirstOrDefault();

        return new CompletionResponse
        {
            Content = choice?.Message?.Content ?? string.Empty,
            PromptTokens = openaiResponse.Usage?.PromptTokens ?? 0,
            CompletionTokens = openaiResponse.Usage?.CompletionTokens ?? 0,
            TotalTokens = openaiResponse.Usage?.TotalTokens ?? 0,
            FinishReason = choice?.FinishReason ?? "unknown",
            Provider = _providerInfo.Id,
            Model = openaiResponse.Model,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(openaiResponse.Created).DateTime
        };
    }

    private string? ExtractDeltaContent(OpenAIStreamChunk chunk)
    {
        return chunk.Choices?.FirstOrDefault()?.Delta?.Content;
    }

    private int EstimateTokenCount(string text)
    {
        return text.Length / 4;
    }
}
