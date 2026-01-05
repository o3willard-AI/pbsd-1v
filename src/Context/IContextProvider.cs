using Microsoft.Extensions.Logging;

namespace PairAdmin.Context;

/// <summary>
/// Interface for context management with sliding window support
/// Provides terminal output context to LLM gateway for AI assistance
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Gets the current terminal output context for LLM processing
    /// </summary>
    /// <param name="maxLines">Maximum number of terminal lines to include in context (default: 100)</param>
    /// <returns>Formatted string with terminal output lines</returns>
    string GetContext(int maxLines = 100);

    /// <summary>
    /// Gets context with default max lines from terminal output buffer
    /// </summary>
    /// <returns>Formatted terminal output context string</returns>
    string GetContext();

    /// <summary>
    /// Gets estimated token count for the context
    /// </summary>
    /// <returns>Estimated number of tokens in context</returns>
    int GetEstimatedTokenCount();

    /// <summary>
    /// Gets the total character count of all context
    /// </summary>
    /// <returns>Total character count</returns>
    int GetTotalCharacterCount();

    /// <summary>
    /// Gets the total line count of all context
    /// </summary>
    /// <returns>Total line count</returns>
    int GetTotalLineCount();

    /// <summary>
    /// Clears the context cache (for new session)
    /// </summary>
    void Clear();

    /// <summary>
    /// Updates the maximum context window size
    /// </summary>
    /// <param name="maxLines">New maximum number of terminal lines</param>
    void SetMaxLines(int maxLines);

    /// <summary>
    /// Enables or disables context caching
    /// </summary>
    /// <param name="enableCache">Whether to cache context</param>
    void SetCacheEnabled(bool enableCache);

    /// <summary>
    /// Gets current cache enabled status
    /// </summary>
    /// <returns>Current cache enabled status</returns>
    bool IsCacheEnabled();

    /// <summary>
    /// Gets current max lines setting
    /// </summary>
    /// <returns>Current max lines setting</returns>
    int GetMaxLines();
}
