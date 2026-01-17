using System;
using System.Threading;

namespace PairAdmin.IoInterceptor.Models;

/// <summary>
/// Statistics for terminal I/O operations
/// </summary>
public class TerminalStatistics
{
    private long _outputEventCount;
    private long _inputEventCount;
    private long _outputBytes;
    private long _inputBytes;
    private long _outputLines;

    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime SessionStartTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity time
    /// </summary>
    public DateTime LastActivityTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Total output event count
    /// </summary>
    public long OutputEventCount => Interlocked.Read(ref _outputEventCount);

    /// <summary>
    /// Total input event count
    /// </summary>
    public long InputEventCount => Interlocked.Read(ref _inputEventCount);

    /// <summary>
    /// Total output bytes received
    /// </summary>
    public long OutputBytes => Interlocked.Read(ref _outputBytes);

    /// <summary>
    /// Total input bytes sent
    /// </summary>
    public long InputBytes => Interlocked.Read(ref _inputBytes);

    /// <summary>
    /// Estimated output lines
    /// </summary>
    public long OutputLines => Interlocked.Read(ref _outputLines);

    /// <summary>
    /// Session duration
    /// </summary>
    public TimeSpan SessionDuration => DateTime.UtcNow - SessionStartTime;

    /// <summary>
    /// Increment output event count and bytes
    /// </summary>
    public void RecordOutput(int bytes, int lines = 0)
    {
        Interlocked.Increment(ref _outputEventCount);
        Interlocked.Add(ref _outputBytes, bytes);
        Interlocked.Add(ref _outputLines, lines);
        LastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Increment input event count and bytes
    /// </summary>
    public void RecordInput(int bytes)
    {
        Interlocked.Increment(ref _inputEventCount);
        Interlocked.Add(ref _inputBytes, bytes);
        LastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset all statistics
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _outputEventCount, 0);
        Interlocked.Exchange(ref _inputEventCount, 0);
        Interlocked.Exchange(ref _outputBytes, 0);
        Interlocked.Exchange(ref _inputBytes, 0);
        Interlocked.Exchange(ref _outputLines, 0);
    }
}
