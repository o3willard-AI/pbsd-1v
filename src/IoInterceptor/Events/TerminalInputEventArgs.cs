using System;

namespace PairAdmin.IoInterceptor.Events;

/// <summary>
/// Event arguments for terminal input events (user commands)
/// </summary>
public class TerminalInputEventArgs : EventArgs
{
    /// <summary>
    /// Timestamp when the input was sent
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Raw byte data of the input
    /// </summary>
    public byte[] RawData { get; init; } = Array.Empty<byte>();

    /// <summary>
    /// Parsed command string (UTF-8 decoded)
    /// </summary>
    public string CommandString { get; init; } = string.Empty;

    /// <summary>
    /// Length of the data in bytes
    /// </summary>
    public int Length => RawData.Length;

    /// <summary>
    /// Whether this appears to be an interactive command (vs paste or special key)
    /// </summary>
    public bool IsInteractive { get; init; }
}
