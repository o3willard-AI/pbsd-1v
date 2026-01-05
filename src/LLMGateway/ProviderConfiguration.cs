namespace PairAdmin.LLMGateway;

/// <summary>
/// Configuration for LLM providers
/// </summary>
public class ProviderConfiguration
{
    /// <summary>
    /// Provider ID (e.g., "openai", "anthropic", "ollama")
    /// </summary>
    public string ProviderId { get; set; }

    /// <summary>
    /// API key for provider
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model to use
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Sampling temperature (0.0 to 2.0)
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Nucleus sampling (0.0 to 1.0)
    /// </summary>
    public double TopP { get; set; }

    /// <summary>
    /// Custom endpoint URL (optional)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Base URL for API
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Additional headers
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Whether to enable streaming by default
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// Maximum context window to use
    /// </summary>
    public int MaxContext { get; set; }

    /// <summary>
    /// Organization ID (for providers like OpenAI)
    /// </summary>
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Additional custom parameters
    /// </summary>
    public Dictionary<string, object>? AdditionalParams { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public ProviderConfiguration()
    {
        ProviderId = string.Empty;
        ApiKey = null;
        Model = string.Empty;
        Temperature = 0.7;
        MaxTokens = 1000;
        TopP = 1.0;
        Endpoint = null;
        MaxRetries = 3;
        Timeout = TimeSpan.FromSeconds(30);
        BaseUrl = null;
        Headers = null;
        EnableStreaming = true;
        MaxContext = 4096;
        OrganizationId = null;
        AdditionalParams = null;
    }

    /// <summary>
    /// Initializes a new instance with provider ID and API key
    /// </summary>
    public ProviderConfiguration(string providerId, string? apiKey) : this()
    {
        ProviderId = providerId;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets a header value
    /// </summary>
    public string? GetHeader(string key)
    {
        return Headers?.TryGetValue(key, out var value) == true ? value : null;
    }

    /// <summary>
    /// Sets a header value
    /// </summary>
    public void SetHeader(string key, string value)
    {
        Headers ??= new Dictionary<string, string>();
        Headers[key] = value;
    }

    /// <summary>
    /// Gets an additional parameter
    /// </summary>
    public T? GetAdditionalParam<T>(string key)
    {
        if (AdditionalParams?.TryGetValue(key, out var value) == true && value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// Sets an additional parameter
    /// </summary>
    public void SetAdditionalParam<T>(string key, T value)
    {
        AdditionalParams ??= new Dictionary<string, object>();
        AdditionalParams[key] = value;
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(ProviderId))
        {
            errorMessage = "Provider ID is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            errorMessage = "Model is required";
            return false;
        }

        if (Temperature < 0.0 || Temperature > 2.0)
        {
            errorMessage = "Temperature must be between 0.0 and 2.0";
            return false;
        }

        if (MaxTokens <= 0)
        {
            errorMessage = "MaxTokens must be positive";
            return false;
        }

        if (TopP < 0.0 || TopP > 1.0)
        {
            errorMessage = "TopP must be between 0.0 and 1.0";
            return false;
        }

        if (Timeout.TotalSeconds <= 0)
        {
            errorMessage = "Timeout must be positive";
            return false;
        }

        if (MaxRetries < 0)
        {
            errorMessage = "MaxRetries must be non-negative";
            return false;
        }

        if (MaxContext <= 0)
        {
            errorMessage = "MaxContext must be positive";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a copy of this configuration
    /// </summary>
    public ProviderConfiguration Clone()
    {
        return new ProviderConfiguration
        {
            ProviderId = ProviderId,
            ApiKey = ApiKey,
            Model = Model,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            TopP = TopP,
            Endpoint = Endpoint,
            MaxRetries = MaxRetries,
            Timeout = Timeout,
            BaseUrl = BaseUrl,
            Headers = Headers?.ToDictionary(h => h.Key, h => h.Value),
            EnableStreaming = EnableStreaming,
            MaxContext = MaxContext,
            OrganizationId = OrganizationId,
            AdditionalParams = AdditionalParams?.ToDictionary(p => p.Key, p => p.Value)
        };
    }

    /// <summary>
    /// Gets a string representation
    /// </summary>
    public override string ToString()
    {
        return $"ProviderConfiguration: {ProviderId}/{Model}, " +
               $"Temperature: {Temperature}, " +
               $"MaxTokens: {MaxTokens}, " +
               $"Retries: {MaxRetries}";
    }
}
