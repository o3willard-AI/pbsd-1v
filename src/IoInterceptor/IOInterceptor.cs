using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using PairAdmin.IoInterceptor.Events;
using PairAdmin.IoInterceptor.Models;

namespace PairAdmin.IoInterceptor;

/// <summary>
/// Main I/O Interceptor module that captures terminal I/O from PuTTY
/// </summary>
public class IOInterceptor : IDisposable
{
    private readonly ILogger<IOInterceptor> _logger;
    private readonly Subject<TerminalOutputEventArgs> _outputSubject;
    private readonly Subject<TerminalInputEventArgs> _inputSubject;
    private readonly TerminalStatistics _statistics;
    private PairAdminCallback? _callbackDelegate;
    private bool _isRegistered;
    private bool _disposed;

    /// <summary>
    /// PairAdmin callback delegate type matching the native signature
    /// </summary>
    /// <param name="eventType">Type of event (1=Output, 2=Input)</param>
    /// <param name="data">Pointer to event data</param>
    /// <param name="length">Length of data</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PairAdminCallback(byte eventType, IntPtr data, int length);

    /// <summary>
    /// Observable stream of terminal output events
    /// </summary>
    public IObservable<TerminalOutputEventArgs> OutputEvents => _outputSubject;

    /// <summary>
    /// Observable stream of terminal input events
    /// </summary>
    public IObservable<TerminalInputEventArgs> InputEvents => _inputSubject;

    /// <summary>
    /// Terminal I/O statistics
    /// </summary>
    public TerminalStatistics Statistics => _statistics;

    /// <summary>
    /// Whether the interceptor is currently registered with PuTTY
    /// </summary>
    public bool IsRegistered => _isRegistered;

    /// <summary>
    /// Creates a new IOInterceptor instance
    /// </summary>
    public IOInterceptor(ILogger<IOInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputSubject = new Subject<TerminalOutputEventArgs>();
        _inputSubject = new Subject<TerminalInputEventArgs>();
        _statistics = new TerminalStatistics();
    }

    /// <summary>
    /// Register callback with PuTTY to start receiving events
    /// </summary>
    public void RegisterCallback()
    {
        if (_isRegistered)
        {
            _logger.LogWarning("Callback already registered");
            return;
        }

        try
        {
            // Create and store delegate to prevent garbage collection
            _callbackDelegate = OnPuTTYCallback;

            // Register with native PuTTY library
            NativeMethods.pairadmin_set_callback(_callbackDelegate);
            _isRegistered = true;

            _logger.LogInformation("PairAdmin callback registered with PuTTY");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register PairAdmin callback");
            throw;
        }
    }

    /// <summary>
    /// Unregister callback from PuTTY
    /// </summary>
    public void UnregisterCallback()
    {
        if (!_isRegistered)
        {
            return;
        }

        try
        {
            NativeMethods.pairadmin_set_callback(null);
            _callbackDelegate = null;
            _isRegistered = false;

            _logger.LogInformation("PairAdmin callback unregistered from PuTTY");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister PairAdmin callback");
        }
    }

    /// <summary>
    /// Get the PuTTY terminal window handle for embedding
    /// </summary>
    public IntPtr GetTerminalWindowHandle()
    {
        try
        {
            return NativeMethods.pairadmin_get_terminal_hwnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get terminal window handle");
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Callback handler invoked by PuTTY for terminal I/O events
    /// </summary>
    private void OnPuTTYCallback(byte eventType, IntPtr data, int length)
    {
        if (length <= 0 || data == IntPtr.Zero)
        {
            return;
        }

        try
        {
            // Copy data from native memory
            var buffer = new byte[length];
            Marshal.Copy(data, buffer, 0, length);

            // Parse as UTF-8 string
            var text = Encoding.UTF8.GetString(buffer);

            if (eventType == 1) // Output
            {
                ProcessOutput(buffer, text);
            }
            else if (eventType == 2) // Input
            {
                ProcessInput(buffer, text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PuTTY callback: eventType={EventType}, length={Length}",
                eventType, length);
        }
    }

    /// <summary>
    /// Process terminal output event
    /// </summary>
    private void ProcessOutput(byte[] data, string text)
    {
        // Count newlines for line statistics
        int lines = 0;
        foreach (char c in text)
        {
            if (c == '\n') lines++;
        }

        // Update statistics
        _statistics.RecordOutput(data.Length, lines);

        // Check for ANSI sequences (basic check for ESC character)
        bool hasAnsi = text.Contains('\x1b');

        // Create event args
        var args = new TerminalOutputEventArgs
        {
            Timestamp = DateTime.UtcNow,
            RawData = data,
            ParsedData = text,
            ContainsAnsiSequences = hasAnsi
        };

        // Publish to subscribers
        _outputSubject.OnNext(args);

        _logger.LogDebug("Terminal output: {Length} bytes, {Lines} lines", data.Length, lines);
    }

    /// <summary>
    /// Process terminal input event
    /// </summary>
    private void ProcessInput(byte[] data, string text)
    {
        // Update statistics
        _statistics.RecordInput(data.Length);

        // Determine if interactive (single character or printable)
        bool isInteractive = data.Length <= 4 || text.All(c => !char.IsControl(c) || c == '\r' || c == '\n');

        // Create event args
        var args = new TerminalInputEventArgs
        {
            Timestamp = DateTime.UtcNow,
            RawData = data,
            CommandString = text,
            IsInteractive = isInteractive
        };

        // Publish to subscribers
        _inputSubject.OnNext(args);

        _logger.LogDebug("Terminal input: {Length} bytes", data.Length);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterCallback();

        _outputSubject.OnCompleted();
        _outputSubject.Dispose();

        _inputSubject.OnCompleted();
        _inputSubject.Dispose();

        _disposed = true;

        _logger.LogInformation("IOInterceptor disposed");
    }

    /// <summary>
    /// Native methods for PuTTY integration
    /// </summary>
    private static class NativeMethods
    {
        private const string DllName = "PairAdminPuTTY";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pairadmin_set_callback(
            [MarshalAs(UnmanagedType.FunctionPtr)] PairAdminCallback? callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pairadmin_get_terminal_hwnd();
    }
}
