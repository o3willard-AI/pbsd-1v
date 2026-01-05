namespace PairAdmin.Context;

/// <summary>
/// Interface for token counting service
/// </summary>
public interface IContextTokenCounter
{
    /// <summary>
    /// Counts the number of tokens in a given text
    /// </summary>
    /// <param name="text">Text to count tokens for</param>
    /// <returns>Estimated number of tokens</returns>
    int CountTokens(string text);

    /// <summary>
    /// Counts the number of tokens in an array of strings
    /// </summary>
    /// <param name="lines">Array of text lines</param>
    /// <returns>Estimated number of tokens</returns>
    int CountTokens(string[] lines);
}

/// <summary>
/// Simple heuristic token counter based on character count
/// Uses a configurable ratio (default: ~4 characters per token)
/// </summary>
public class ContextTokenCounter : IContextTokenCounter
{
    private readonly double _charactersPerToken;

    /// <summary>
    /// Initializes a new instance with default token ratio (4 chars/token)
    /// </summary>
    public ContextTokenCounter() : this(4.0)
    {
    }

    /// <summary>
    /// Initializes a new instance with custom token ratio
    /// </summary>
    /// <param name="charactersPerToken">Average characters per token</param>
    public ContextTokenCounter(double charactersPerToken)
    {
        if (charactersPerToken <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charactersPerToken), "Characters per token must be positive");
        }

        _charactersPerToken = charactersPerToken;
    }

    /// <summary>
    /// Counts the number of tokens in a given text
    /// </summary>
    /// <param name="text">Text to count tokens for</param>
    /// <returns>Estimated number of tokens</returns>
    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var charCount = text.Length;
        return (int)Math.Ceiling(charCount / _charactersPerToken);
    }

    /// <summary>
    /// Counts the number of tokens in an array of strings
    /// </summary>
    /// <param name="lines">Array of text lines</param>
    /// <returns>Estimated number of tokens</returns>
    public int CountTokens(string[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return 0;
        }

        int totalTokens = 0;
        foreach (var line in lines)
        {
            totalTokens += CountTokens(line);
        }

        return totalTokens;
    }
}
