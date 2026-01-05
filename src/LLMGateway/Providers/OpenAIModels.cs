namespace PairAdmin.LLMGateway.Providers;

/// <summary>
/// OpenAI completion request model
/// </summary>
public class OpenAICompletionRequest
{
    /// <summary>
    /// Model to use
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Messages in the conversation
    /// </summary>
    public List<OpenAIMessage> Messages { get; set; }

    /// <summary>
    /// Sampling temperature (0.0 to 2.0)
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Nucleus sampling (0.0 to 1.0)
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Number of completions to generate
    /// </summary>
    public int? N { get; set; }

    /// <summary>
    /// Presence penalty (0.0 to 2.0)
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Frequency penalty (0.0 to 2.0)
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Stop sequences
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public OpenAICompletionRequest()
    {
        Model = string.Empty;
        Messages = new List<OpenAIMessage>();
    }
}

/// <summary>
/// OpenAI message model
/// </summary>
public class OpenAIMessage
{
    /// <summary>
    /// Message role (system, user, assistant)
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; }
}

/// <summary>
/// OpenAI completion response model
/// </summary>
public class OpenAICompletionResponse
{
    /// <summary>
    /// Response ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Object type
    /// </summary>
    public string Object { get; set; }

    /// <summary>
    /// Creation timestamp (Unix epoch)
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// Model used
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Completion choices
    /// </summary>
    public List<OpenAIChoice> Choices { get; set; }

    /// <summary>
    /// Token usage
    /// </summary>
    public OpenAIUsage? Usage { get; set; }
}

/// <summary>
/// OpenAI choice model
/// </summary>
public class OpenAIChoice
{
    /// <summary>
    /// Choice index
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Generated message
    /// </summary>
    public OpenAIMessage? Message { get; set; }

    /// <summary>
    /// Delta content (for streaming)
    /// </summary>
    public OpenAIMessage? Delta { get; set; }

    /// <summary>
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }
}

/// <summary>
/// OpenAI usage model
/// </summary>
public class OpenAIUsage
{
    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// OpenAI error response model
/// </summary>
public class OpenAIErrorResponse
{
    /// <summary>
    /// Error details
    /// </summary>
    public OpenAIError? Error { get; set; }
}

/// <summary>
/// OpenAI error model
/// </summary>
public class OpenAIError
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Error type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Error parameter (if applicable)
    /// </summary>
    public string? Param { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? Code { get; set; }
}

/// <summary>
/// OpenAI stream chunk model
/// </summary>
public class OpenAIStreamChunk
{
    /// <summary>
    /// Response ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Object type
    /// </summary>
    public string? Object { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public long? Created { get; set; }

    /// <summary>
    /// Model used
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Choices
    /// </summary>
    public List<OpenAIChoice>? Choices { get; set; }

    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsDone { get; set; }
}
