using PairAdmin.Context;

namespace PairAdmin.LLMGateway;

/// <summary>
/// Interface for LLM providers
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Gets provider information
    /// </summary>
    /// <returns>Provider metadata</returns>
    Models.Provider GetProviderInfo();

    /// <summary>
    /// Configures the provider
    /// </summary>
    /// <param name="configuration">Provider configuration</param>
    void Configure(ProviderConfiguration configuration);

    /// <summary>
    /// Checks if provider is configured
    /// </summary>
    /// <returns>True if configured</returns>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completion response</returns>
    /// <exception cref="LLMProviderException">Provider-specific error</exception>
    Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming completion request
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming responses</returns>
    /// <exception cref="LLMProviderException">Provider-specific error</exception>
    IAsyncEnumerable<StreamingCompletionResponse> StreamCompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates token count for text
    /// </summary>
    /// <param name="text">Text to count</param>
    /// <returns>Estimated token count</returns>
    int EstimateTokenCount(string text);

    /// <summary>
    /// Tests provider connection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates configuration
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <param name="errorMessage">Error message if invalid</param>
    /// <returns>True if configuration is valid</returns>
    bool ValidateConfiguration(
        ProviderConfiguration configuration,
        out string? errorMessage);

    /// <summary>
    /// Gets supported models for this provider
    /// </summary>
    /// <returns>List of supported model names</returns>
    List<string> GetSupportedModels();

    /// <summary>
    /// Gets default model for this provider
    /// </summary>
    /// <returns>Default model name</returns>
    string GetDefaultModel();

    /// <summary>
    /// Gets whether provider supports streaming
    /// </summary>
    /// <returns>True if streaming is supported</returns>
    bool SupportsStreaming();

    /// <summary>
    /// Gets the maximum context window for the current configuration
    /// </summary>
    /// <returns>Maximum tokens in context</returns>
    int GetMaxContext();
}
