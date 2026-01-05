namespace PairAdmin.LLMGateway;

/// <summary>
/// Streaming completion response chunk
/// </summary>
public class StreamingCompletionResponse
{
    /// <summary>
    /// Content chunk
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Whether the stream is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Reason for completion (if complete)
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// Cumulative token count
    /// </summary>
    public int CumulativeTokens { get; set; }

    /// <summary>
    /// Provider generating the response
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Model being used
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Index of this chunk
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Timestamp of this chunk
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Whether this chunk is empty
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public StreamingCompletionResponse()
    {
        Content = string.Empty;
        IsComplete = false;
        CumulativeTokens = 0;
        ChunkIndex = 0;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Initializes a new instance with content
    /// </summary>
    public StreamingCompletionResponse(string content, int chunkIndex = 0) : this()
    {
        Content = content;
        ChunkIndex = chunkIndex;
    }

    /// <summary>
    /// Creates a complete (final) chunk
    /// </summary>
    public static StreamingCompletionResponse Complete(string finishReason = "stop")
    {
        return new StreamingCompletionResponse
        {
            Content = string.Empty,
            IsComplete = true,
            FinishReason = finishReason
        };
    }

    /// <summary>
    /// Creates an error chunk
    /// </summary>
    public static StreamingCompletionResponse Error(string errorMessage)
    {
        return new StreamingCompletionResponse
        {
            Content = $"[Error: {errorMessage}]",
            IsComplete = true,
            FinishReason = "error"
        };
    }

    /// <summary>
    /// Gets a string representation
    /// </summary>
    public override string ToString()
    {
        if (IsComplete)
        {
            return $"Stream complete. Reason: {FinishReason ?? "unknown"}, Total tokens: {CumulativeTokens}";
        }

        return $"Chunk {ChunkIndex}: {Content?.Substring(0, Math.Min(20, Content?.Length ?? 0))}...";
    }

    /// <summary>
    /// Updates cumulative token count
    /// </summary>
    public void UpdateCumulativeTokens(int tokensPerChunk = 4)
    {
        if (!string.IsNullOrWhiteSpace(Content))
        {
            CumulativeTokens += Content.Length / tokensPerChunk;
        }
    }

    /// <summary>
    /// Gets the chunk for appending to message
    /// </summary>
    public string GetAppendContent()
    {
        return Content ?? string.Empty;
    }

    /// <summary>
    /// Creates a copy of this chunk
    /// </summary>
    public StreamingCompletionResponse Clone()
    {
        return new StreamingCompletionResponse
        {
            Content = Content,
            IsComplete = IsComplete,
            FinishReason = FinishReason,
            CumulativeTokens = CumulativeTokens,
            Provider = Provider,
            Model = Model,
            ChunkIndex = ChunkIndex,
            Timestamp = Timestamp
        };
    }

    /// <summary>
    /// Gets whether this is the last chunk
    /// </summary>
    public bool IsLast => IsComplete;

    /// <summary>
    /// Gets whether this chunk contains data
    /// </summary>
    public bool HasData => !IsComplete && !IsEmpty;
}
