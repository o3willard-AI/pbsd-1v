using System;

namespace PairAdmin.IoInterceptor.Events;

/// <summary>
/// Event arguments for terminal output events from PuTTY
/// </summary>
public class TerminalOutputEventArgs : EventArgs
{
    /// <summary>
    /// Timestamp when the output was received
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Raw byte data from the terminal
    /// </summary>
    public byte[] RawData { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Parsed string data (UTF-8 decoded)
    /// </summary>
    public string ParsedData { get; init; } = string.Empty;

    /// <summary>
    /// Length of the data in bytes
    /// </summary>
    public int Length => RawData.Length;

    /// <summary>
    /// Whether the data contains ANSI escape sequences
    /// </summary>
    public bool ContainsAnsiSequences { get; init; }
}
