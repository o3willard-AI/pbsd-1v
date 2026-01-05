namespace PairAdmin.LLMGateway;

/// <summary>
/// Base exception for LLM Gateway errors
/// </summary>
public class LLMGatewayException : Exception
{
    /// <summary>
    /// Provider that raised the error
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Error code from provider
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// HTTP status code (if applicable)
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Whether error is retryable
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMGatewayException() : base("LLM Gateway error")
    {
        IsRetryable = false;
    }

    /// <summary>
    /// Initializes a new instance with message
    /// </summary>
    public LLMGatewayException(string message) : base(message)
    {
        IsRetryable = false;
    }

    /// <summary>
    /// Initializes a new instance with message and inner exception
    /// </summary>
    public LLMGatewayException(string message, Exception innerException)
        : base(message, innerException)
    {
        IsRetryable = false;
    }

    /// <summary>
    /// Initializes a new instance with full details
    /// </summary>
    public LLMGatewayException(
        string message,
        string? provider,
        string? errorCode,
        int? httpStatusCode,
        bool isRetryable,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Provider = provider;
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
        IsRetryable = isRetryable;
    }
}

/// <summary>
/// Exception for provider-specific errors
/// </summary>
public class LLMProviderException : LLMGatewayException
{
    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMProviderException(string provider, string message)
        : base(message)
    {
        Provider = provider;
    }

    /// <summary>
    /// Initializes a new instance with inner exception
    /// </summary>
    public LLMProviderException(string provider, string message, Exception innerException)
        : base(message, innerException)
    {
        Provider = provider;
    }

    /// <summary>
    /// Initializes a new instance with full details
    /// </summary>
    public LLMProviderException(
        string provider,
        string message,
        string? errorCode,
        int? httpStatusCode,
        bool isRetryable,
        Exception? innerException = null)
        : base(message, provider, errorCode, httpStatusCode, isRetryable, innerException)
    {
    }
}

/// <summary>
/// Exception for configuration errors
/// </summary>
public class LLMConfigurationException : LLMGatewayException
{
    /// <summary>
    /// Configuration key that caused error
    /// </summary>
    public string? ConfigurationKey { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with key
    /// </summary>
    public LLMConfigurationException(string key, string message) : base(message)
    {
        ConfigurationKey = key;
    }
}

/// <summary>
/// Exception for authentication errors
/// </summary>
public class LLMAuthenticationException : LLMProviderException
{
    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMAuthenticationException(string provider, string message)
        : base(provider, message)
    {
        IsRetryable = false;
    }
}

/// <summary>
/// Exception for rate limit errors
/// </summary>
public class LLMRateLimitException : LLMProviderException
{
    /// <summary>
    /// Time until retry allowed
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMRateLimitException(string provider, string message)
        : base(provider, message)
    {
        IsRetryable = true;
    }

    /// <summary>
    /// Initializes a new instance with retry after
    /// </summary>
    public LLMRateLimitException(string provider, string message, TimeSpan retryAfter)
        : this(provider, message)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Exception for timeout errors
/// </summary>
public class LLMTimeoutException : LLMProviderException
{
    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMTimeoutException(string provider, string message, TimeSpan timeout)
        : base(provider, message)
    {
        Timeout = timeout;
        IsRetryable = true;
    }
}

/// <summary>
/// Exception for token limit errors
/// </summary>
public class LLMTokenLimitException : LLMProviderException
{
    /// <summary>
    /// Token count that exceeded limit
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Maximum allowed tokens
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMTokenLimitException(string provider, int tokenCount, int maxTokens)
        : base(provider, $"Token limit exceeded: {tokenCount}/{maxTokens}")
    {
        TokenCount = tokenCount;
        MaxTokens = maxTokens;
        IsRetryable = false;
    }
}

/// <summary>
/// Exception for invalid request errors
/// </summary>
public class LLMInvalidRequestException : LLMProviderException
{
    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMInvalidRequestException(string provider, string message)
        : base(provider, message)
    {
        IsRetryable = false;
    }
}

/// <summary>
/// Exception when provider is not registered
/// </summary>
public class LLMProviderNotFoundException : LLMGatewayException
{
    /// <summary>
    /// Provider ID that was not found
    /// </summary>
    public string ProviderId { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMProviderNotFoundException(string providerId)
        : base($"Provider '{providerId}' is not registered")
    {
        ProviderId = providerId;
        IsRetryable = false;
    }
}

/// <summary>
/// Exception when active provider is not set
/// </summary>
public class LLMNoActiveProviderException : LLMGatewayException
{
    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public LLMNoActiveProviderException()
        : base("No active LLM provider is configured")
    {
        IsRetryable = false;
    }
}
