namespace PairAdmin.Context;

/// <summary>
/// Result of context truncation
/// </summary>
public class TruncationResult
{
    /// <summary>
    /// Truncated context string
    /// </summary>
    public string TruncatedContext { get; set; }

    /// <summary>
    /// Original token count before truncation
    /// </summary>
    public int OriginalTokens { get; set; }

    /// <summary>
    /// Token count after truncation
    /// </summary>
    public int TruncatedTokens { get; set; }

    /// <summary>
    /// Number of tokens removed
    /// </summary>
    public int RemovedTokens => OriginalTokens - TruncatedTokens;

    /// <summary>
    /// Whether context was truncated
    /// </summary>
    public bool WasTruncated { get; set; }

    /// <summary>
    /// Reason for truncation
    /// </summary>
    public TruncationReason Reason { get; set; }

    /// <summary>
    /// Percentage of context kept
    /// </summary>
    public double KeptPercentage => OriginalTokens > 0 ? (double)TruncatedTokens / OriginalTokens : 1.0;

    /// <summary>
    /// Gets a string representation
    /// </summary>
    public override string ToString()
    {
        if (!WasTruncated)
        {
            return $"No truncation: {TruncatedTokens} tokens";
        }

        return $"Truncated: {TruncatedTokens}/{OriginalTokens} tokens " +
               $"({KeptPercentage:P1}), Reason: {Reason}";
    }
}
