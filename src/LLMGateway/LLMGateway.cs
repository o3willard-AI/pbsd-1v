using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PairAdmin.Context;

namespace PairAdmin.LLMGateway;

/// <summary>
/// Main gateway for LLM provider communication
/// </summary>
public class LLMGateway
{
    private readonly ConcurrentDictionary<string, ILLMProvider> _providers;
    private readonly ConcurrentDictionary<string, ProviderConfiguration> _configurations;
    private ILLMProvider? _activeProvider;
    private readonly ILogger<LLMGateway> _logger;
    private readonly IContextProvider? _contextProvider;

    /// <summary>
    /// Gets registered providers
    /// </summary>
    public IReadOnlyDictionary<string, ILLMProvider> Providers => _providers;

    /// <summary>
    /// Gets active provider
    /// </summary>
    public ILLMProvider? ActiveProvider => _activeProvider;

    /// <summary>
    /// Gets active provider ID
    /// </summary>
    public string? ActiveProviderId => _activeProvider?.GetProviderInfo().Id;

    /// <summary>
    /// Event raised when message is sent
    /// </summary>
    public event EventHandler<CompletionRequest>? RequestSent;

    /// <summary>
    /// Event raised when response is received
    /// </summary>
    public event EventHandler<CompletionResponse>? ResponseReceived;

    /// <summary>
    /// Event raised when streaming chunk is received
    /// </summary>
    public event EventHandler<StreamingCompletionResponse>? StreamingChunkReceived;

    /// <summary>
    /// Event raised when error occurs
    /// </summary>
    public event EventHandler<LLMGatewayException>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of LLMGateway
    /// </summary>
    public LLMGateway(
        ILogger<LLMGateway> logger,
        IContextProvider? contextProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _contextProvider = contextProvider;
        _providers = new ConcurrentDictionary<string, ILLMProvider>();
        _configurations = new ConcurrentDictionary<string, ProviderConfiguration>();

        _logger.LogInformation("LLMGateway initialized");
    }

    /// <summary>
    /// Registers a new provider
    /// </summary>
    /// <param name="provider">Provider to register</param>
    public void RegisterProvider(ILLMProvider provider)
    {
        var info = provider.GetProviderInfo();

        if (string.IsNullOrWhiteSpace(info.Id))
        {
            throw new LLMConfigurationException("Provider ID cannot be empty");
        }

        _providers.TryAdd(info.Id, provider);
        _logger.LogInformation($"Provider registered: {info.Id} ({info.Name})");
    }

    /// <summary>
    /// Unregisters a provider
    /// </summary>
    /// <param name="providerId">Provider ID to unregister</param>
    /// <returns>True if provider was unregistered</returns>
    public bool UnregisterProvider(string providerId)
    {
        if (_providers.TryRemove(providerId, out _))
        {
            _configurations.TryRemove(providerId, out _);

            if (_activeProvider?.GetProviderInfo().Id == providerId)
            {
                _activeProvider = null;
                _logger.LogWarning("Unregistered active provider");
            }

            _logger.LogInformation($"Provider unregistered: {providerId}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Configures a provider
    /// </summary>
    /// <param name="configuration">Provider configuration</param>
    public void ConfigureProvider(ProviderConfiguration configuration)
    {
        if (!configuration.Validate(out var errorMessage))
        {
            throw new LLMConfigurationException(
                $"Invalid configuration for provider {configuration.ProviderId}: {errorMessage}");
        }

        if (!_providers.TryGetValue(configuration.ProviderId, out var provider))
        {
            throw new LLMProviderNotFoundException(configuration.ProviderId);
        }

        provider.Configure(configuration);
        _configurations[configuration.ProviderId] = configuration;
        _logger.LogInformation($"Provider configured: {configuration}");
    }

    /// <summary>
    /// Sets the active provider
    /// </summary>
    /// <param name="providerId">Provider ID to set as active</param>
    public void SetActiveProvider(string providerId)
    {
        if (!_providers.TryGetValue(providerId, out var provider))
        {
            throw new LLMProviderNotFoundException(providerId);
        }

        if (!_configurations.TryGetValue(providerId, out _))
        {
            _logger.LogWarning($"Setting active provider without configuration: {providerId}");
        }

        _activeProvider = provider;
        _logger.LogInformation($"Active provider set: {providerId}");
    }

    /// <summary>
    /// Gets a configuration for a provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>Configuration or null if not found</returns>
    public ProviderConfiguration? GetConfiguration(string providerId)
    {
        return _configurations.TryGetValue(providerId, out var config) ? config : null;
    }

    /// <summary>
    /// Gets the active configuration
    /// </summary>
    /// <returns>Active configuration or null</returns>
    public ProviderConfiguration? GetActiveConfiguration()
    {
        return _activeProvider != null ? GetConfiguration(_activeProvider.GetProviderInfo().Id) : null;
    }

    /// <summary>
    /// Sends a completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    public async Task<CompletionResponse> SendAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_activeProvider == null)
        {
            throw new LLMNoActiveProviderException();
        }

        if (!_request.IsValid(out var errorMessage))
        {
            throw new LLMConfigurationException($"Invalid request: {errorMessage}");
        }

        await AddContextToRequestAsync(request);

        RequestSent?.Invoke(this, request);

        var response = await RetryWithBackoffAsync(
            () => _activeProvider.CompleteAsync(request, cancellationToken),
            cancellationToken);

        ResponseReceived?.Invoke(this, response);

        _logger.LogInformation($"Response received. Tokens: {response.TotalTokens}, " +
                           $"Provider: {response.Provider}");

        return response;
    }

    /// <summary>
    /// Sends a streaming completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming responses</returns>
    public async IAsyncEnumerable<StreamingCompletionResponse> SendStreamingAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_activeProvider == null)
        {
            throw new LLMNoActiveProviderException();
        }

        if (!_activeProvider.SupportsStreaming())
        {
            throw new LLMGatewayException(
                $"Provider '{ActiveProviderId}' does not support streaming");
        }

        if (!_request.IsValid(out var errorMessage))
        {
            throw new LLMConfigurationException($"Invalid request: {errorMessage}");
        }

        await AddContextToRequestAsync(request);

        RequestSent?.Invoke(this, request);

        var retries = GetActiveConfiguration()?.MaxRetries ?? 3;
        var attempt = 0;
        var success = false;
        StreamingCompletionResponse? lastError = null;

        while (attempt < retries && !success && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                await foreach (var chunk in _activeProvider.StreamCompleteAsync(
                    request,
                    cancellationToken))
                {
                    StreamingChunkReceived?.Invoke(this, chunk);
                    yield return chunk;

                    if (chunk.IsComplete)
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    _logger.LogInformation($"Streaming completed after {attempt} attempts");
                }
            }
            catch (LLMProviderException ex) when (ex.IsRetryable && attempt < retries)
            {
                lastError = StreamingCompletionResponse.Error(ex.Message);
                var delay = CalculateBackoffDelay(attempt);
                _logger.LogWarning($"Retry attempt {attempt}/{retries} after {delay.TotalSeconds}s. " +
                                  $"Error: {ex.Message}");
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastError = StreamingCompletionResponse.Error(ex.Message);
                ErrorOccurred?.Invoke(this, new LLMGatewayException(
                    $"Streaming failed: {ex.Message}", ex));
                yield return lastError;
                yield break;
            }
        }

        if (!success && lastError != null)
        {
            yield return lastError;
        }
    }

    /// <summary>
    /// Tests connection to active provider
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    public async Task<bool> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_activeProvider == null)
        {
            _logger.LogWarning("Cannot test connection: no active provider");
            return false;
        }

        try
        {
            var result = await _activeProvider.TestConnectionAsync(cancellationToken);
            _logger.LogInformation($"Connection test result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Estimates token count for text
    /// </summary>
    /// <param name="text">Text to count</param>
    /// <returns>Estimated token count</returns>
    public int EstimateTokenCount(string text)
    {
        return _activeProvider?.EstimateTokenCount(text) ?? text.Length / 4;
    }

    /// <summary>
    /// Gets all registered provider IDs
    /// </summary>
    /// <returns>List of provider IDs</returns>
    public List<string> GetProviderIds()
    {
        return _providers.Keys.ToList();
    }

    /// <summary>
    /// Gets provider information for all providers
    /// </summary>
    /// <returns>List of provider info</returns>
    public List<Models.Provider> GetAllProvidersInfo()
    {
        return _providers.Values.Select(p => p.GetProviderInfo()).ToList();
    }

    private CompletionRequest _request;

    private async Task AddContextToRequestAsync(CompletionRequest request)
    {
        if (_contextProvider != null && request.Context == null)
        {
            try
            {
                var context = _contextProvider.GetContext(request.MaxTokens / 2);
                request.AddContext(context);
                _logger.LogTrace($"Added {context.Length} chars of context to request");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add context to request");
            }
        }
    }

    private async Task<CompletionResponse> RetryWithBackoffAsync(
        Func<Task<CompletionResponse>> action,
        CancellationToken cancellationToken)
    {
        var config = GetActiveConfiguration();
        var maxRetries = config?.MaxRetries ?? 3;
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                return await action();
            }
            catch (LLMProviderException ex) when (ex.IsRetryable && attempt < maxRetries)
            {
                lastException = ex;
                var delay = CalculateBackoffDelay(attempt);
                _logger.LogWarning($"Retry attempt {attempt}/{maxRetries} after " +
                                  $"{delay.TotalSeconds}s. Error: {ex.Message}");
                await Task.Delay(delay, cancellationToken);
            }
            catch (LLMProviderException ex)
            {
                lastException = ex;
                break;
            }
        }

        if (lastException != null)
        {
            ErrorOccurred?.Invoke(this, lastException);
            throw lastException;
        }

        throw new LLMGatewayException("Retry limit exceeded", lastException);
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        var delayMs = (long)(Math.Pow(2, attempt - 1) * 1000);
        var jitter = new Random().Next(0, 1000);
        return TimeSpan.FromMilliseconds(delayMs + jitter);
    }
}
