namespace PairAdmin.LLMGateway;

/// <summary>
/// Completion response from LLM API
/// </summary>
public class CompletionResponse
{
    /// <summary>
    /// Generated content
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Token count for prompt
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Token count for completion
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total token count
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Reason for completion (stop, length, etc.)
    /// </summary>
    public string FinishReason { get; set; }

    /// <summary>
    /// Provider that generated the response
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Model used for generation
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Additional metadata from provider
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Timestamp when response was received
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Whether the completion was successful
    /// </summary>
    public bool IsSuccess => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public CompletionResponse()
    {
        Content = string.Empty;
        PromptTokens = 0;
        CompletionTokens = 0;
        TotalTokens = 0;
        FinishReason = "unknown";
        Metadata = new Dictionary<string, object>();
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Initializes a new instance with content
    /// </summary>
    public CompletionResponse(string content) : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets a metadata value
    /// </summary>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata != null && Metadata.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    /// <summary>
    /// Sets a metadata value
    /// </summary>
    public void SetMetadata<T>(string key, T value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
    }

    /// <summary>
    /// Gets the token cost estimate (USD)
    /// </summary>
    /// <param name="inputCost">Cost per million input tokens</param>
    /// <param name="outputCost">Cost per million output tokens</param>
    /// <returns>Estimated cost in USD</returns>
    public double GetEstimatedCost(double inputCost = 0.0015, double outputCost = 0.002)
    {
        var inputCostDollars = (PromptTokens / 1_000_000.0) * inputCost;
        var outputCostDollars = (CompletionTokens / 1_000_000.0) * outputCost;
        return inputCostDollars + outputCostDollars;
    }

    /// <summary>
    /// Converts response to ChatMessage
    /// </summary>
    public Chat.ChatMessage ToChatMessage()
    {
        return new Chat.ChatMessage
        {
            Sender = Chat.MessageSender.Assistant,
            Content = Content,
            Timestamp = Timestamp ?? DateTime.Now,
            TokenCount = TotalTokens
        };
    }

    /// <summary>
    /// Gets a string representation
    /// </summary>
    public override string ToString()
    {
        return $"CompletionResponse: {Content?.Substring(0, Math.Min(50, Content?.Length ?? 0))}... " +
               $"Tokens: {TotalTokens} " +
               $"Provider: {Provider ?? "unknown"} " +
               $"Model: {Model ?? "unknown"}";
    }
}
