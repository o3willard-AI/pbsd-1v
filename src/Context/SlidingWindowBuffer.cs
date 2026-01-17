using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PairAdmin.DataStructures;

namespace PairAdmin.Context;

/// <summary>
/// Sliding window buffer for storing and retrieving terminal output
/// </summary>
public class SlidingWindowBuffer
{
    private readonly CircularBuffer<string> _buffer;
    private readonly ILogger _logger;
    private readonly int _maxLines;

    /// <summary>
    /// Initializes a new instance of sliding window buffer
    /// </summary>
    public SlidingWindowBuffer(ILogger logger, int maxLines = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buffer = new CircularBuffer<string>(maxLines);
        _maxLines = maxLines;
    }

    /// <summary>
    /// Adds terminal output line to the buffer
    /// </summary>
    /// <param name="line">Terminal output line to add</param>
    public void AddLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _buffer.Add(line);
        
        _logger.LogTrace($"Added line to buffer. Count: {_buffer.Count}, Capacity: {_buffer.Capacity}");
    }

    /// <summary>
    /// Retrieves the last N lines from the buffer (for AI context)
    /// </summary>
    /// <param name="count">Number of lines to retrieve (N lines)</param>
    /// <returns>Array of last N lines (oldest to newest)</returns>
    public string[] GetLastNLines(int count)
    {
        if (count <= 0 || count > _buffer.Count)
        {
            count = _buffer.Count;
        }

        if (_buffer.Count == 0)
        {
            _logger.LogWarning($"Cannot retrieve {count} lines: buffer is empty");
            return Array.Empty<string>();
        }

        var lines = _buffer.TryPeekFromStart(count);
        _logger.LogTrace($"Retrieved {count} lines from buffer");
        
        return lines;
    }

    /// <summary>
    /// Retrieves all lines in the buffer
    /// </summary>
    /// <returns>All lines in buffer (oldest to newest)</returns>
    public string[] GetAllLines()
    {
        if (_buffer.Count == 0)
        {
            _logger.LogWarning("Cannot retrieve lines: buffer is empty");
            return Array.Empty<string>();
        }

        var lines = _buffer.TryPeekFromStart(_buffer.Count);
        return lines;
    }

    /// <summary>
    /// Clears the buffer (useful for starting new session)
    /// </summary>
    public void Clear()
    {
        var count = _buffer.Count;
        _buffer.Clear();
        _logger.LogInformation($"Cleared buffer. Removed {count} lines");
    }

    /// <summary>
    /// Gets the current number of lines in the buffer
    /// </summary>
    /// <returns>Current line count</returns>
    public int LineCount => _buffer.Count;

    /// <summary>
    /// Gets the maximum number of lines the buffer can hold
    /// </summary>
    /// <returns>Maximum capacity</returns>
    public int MaxLines => _maxLines;

    /// <summary>
    /// Gets the context string for the last N lines (for LLM prompt)
    /// </summary>
    /// <param name="count">Number of lines to include in context</param>
    /// <returns>Formatted context string</returns>
    public string GetContextString(int count)
    {
        var lines = GetLastNLines(count);
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Gets the total character count of all lines in the buffer
    /// </summary>
    /// <returns>Total character count</returns>
    public int GetTotalCharacterCount()
    {
        var lines = GetAllLines();
        return lines.Sum(line => line.Length);
    }

    /// <summary>
    /// Estimates the token count of the context (approximately)
    /// </summary>
    /// <returns>Estimated token count</returns>
    public int GetEstimatedTokenCount()
    {
        var charCount = GetTotalCharacterCount();
        // Approximate: 4 characters per token (rule of thumb)
        return charCount / 4;
    }
}
