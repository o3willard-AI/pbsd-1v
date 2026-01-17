using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PairAdmin.Interop;
using PairAdmin.UI.Terminal;
using PairAdmin.UI.Dialogs;

namespace PairAdmin.UI.Controls;

/// <summary>
/// Simple file logger for debugging TerminalPane
/// </summary>
internal static class TerminalDebugLog
{
    private static readonly object _lock = new();
    private static StreamWriter? _writer;
    private static string? _logPath;

    public static string? LogPath => _logPath;

    public static void Initialize()
    {
        if (_writer != null) return;

        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PairAdmin");
            Directory.CreateDirectory(logDir);

            _logPath = Path.Combine(logDir, "pairadmin_csharp.log");
            _writer = new StreamWriter(_logPath, append: true) { AutoFlush = true };

            Log("========================================");
            Log($"PairAdmin C# Log Started: {DateTime.Now}");
            Log($"Process ID: {Environment.ProcessId}");
            Log("========================================");
        }
        catch { }
    }

    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
        }
        catch { }
    }

    public static void LogException(Exception ex, string context)
    {
        Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}");
        Log($"  Stack: {ex.StackTrace}");
    }

    public static void Close()
    {
        lock (_lock)
        {
            _writer?.Close();
            _writer = null;
        }
    }
}

public partial class TerminalPane : UserControl, IDisposable
{
    private readonly ILogger<TerminalPane> _logger;
    private TerminalSettings _settings;
    private IntPtr _puttyWindowHandle = IntPtr.Zero;
    private Process? _puttyProcess;
    private bool _isConnected;
    private bool _disposed;
    private bool _settingsChecked;

    // Callback delegate must be kept alive to prevent GC
    private PuTTYInterop.PairAdminCallback? _callbackDelegate;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<TerminalOutputEventArgs>? TerminalOutput;
    public event EventHandler<TerminalInputEventArgs>? TerminalInput;

    public bool IsConnected => _isConnected;
    public IntPtr WindowHandle => _puttyWindowHandle;
    public TerminalMode CurrentMode => _settings.Mode;

    public TerminalPane() : this(NullLogger<TerminalPane>.Instance)
    {
    }

    public TerminalPane(ILogger<TerminalPane> logger)
    {
        TerminalDebugLog.Initialize();
        TerminalDebugLog.Log("TerminalPane constructor called");

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = TerminalSettings.Load();
        TerminalDebugLog.Log($"Settings loaded: Mode={_settings.Mode}");

        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;

        TerminalDebugLog.Log("TerminalPane constructor complete");
    }

    private void InitializeTerminal()
    {
        TerminalDebugLog.Log("InitializeTerminal called");
        _logger.LogInformation("Initializing TerminalPane");

        // Check settings on first load
        if (!_settingsChecked)
        {
            _settingsChecked = true;
            CheckTerminalSettings();
        }

        // Restore last connection info
        if (!string.IsNullOrEmpty(_settings.LastHostname))
        {
            HostnameTextBox.Text = _settings.LastHostname;
            PortTextBox.Text = _settings.LastPort.ToString();
            UsernameTextBox.Text = _settings.LastUsername ?? "";
        }

        UpdateModeIndicator();
    }

    private void CheckTerminalSettings()
    {
        if (_settings.Mode == TerminalMode.NotConfigured)
        {
            // Show setup dialog on first launch
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var result = TerminalSetupDialog.ShowSetupDialog();
                if (result.HasValue)
                {
                    _settings = TerminalSettings.Load(); // Reload after save
                    UpdateModeIndicator();
                    _logger.LogInformation("Terminal mode configured: {Mode}", _settings.Mode);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void UpdateModeIndicator()
    {
        Dispatcher.Invoke(() =>
        {
            var modeText = _settings.Mode switch
            {
                TerminalMode.Integrated => " [Integrated]",
                TerminalMode.External => " [External PuTTY]",
                _ => " [Not Configured]"
            };

            // Update placeholder text based on mode
            if (_settings.Mode == TerminalMode.NotConfigured)
            {
                if (PlaceholderPanel.Children.Count > 1 && PlaceholderPanel.Children[1] is TextBlock tb)
                {
                    tb.Text = "Click Connect to configure terminal mode";
                }
            }
        });
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        // Check if terminal mode is configured
        if (_settings.Mode == TerminalMode.NotConfigured)
        {
            var result = TerminalSetupDialog.ShowSetupDialog();
            if (!result.HasValue)
                return;
            _settings = TerminalSettings.Load();
        }

        var hostname = HostnameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(hostname))
        {
            MessageBox.Show("Please enter a hostname.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(PortTextBox.Text, out int port) || port < 1 || port > 65535)
        {
            MessageBox.Show("Please enter a valid port number (1-65535).", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var username = UsernameTextBox.Text.Trim();

        // Save connection info
        _settings.LastHostname = hostname;
        _settings.LastPort = port;
        _settings.LastUsername = username;
        _settings.Save();

        // Connect based on mode
        if (_settings.Mode == TerminalMode.Integrated)
        {
            ConnectIntegrated(hostname, port, username);
        }
        else
        {
            ConnectExternal(hostname, port, username);
        }
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        Disconnect();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Disconnect first if connected
        if (_isConnected)
        {
            var result = MessageBox.Show(
                "Changing settings will disconnect the current session.\n\nContinue?",
                "Disconnect Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            Disconnect();
        }

        // Show the terminal setup dialog
        var dialogResult = TerminalSetupDialog.ShowSetupDialog();
        if (dialogResult.HasValue)
        {
            _settings = TerminalSettings.Load();
            UpdateModeIndicator();
            _logger.LogInformation("Terminal mode changed to: {Mode}", _settings.Mode);
        }
    }

    #region Integrated Mode (PairAdminPuTTY.dll)

    private void ConnectIntegrated(string hostname, int port, string username)
    {
        TerminalDebugLog.Log("=== ConnectIntegrated START ===");
        TerminalDebugLog.Log($"hostname={hostname} port={port} username={username}");

        if (_isConnected) Disconnect();
        _logger.LogInformation("Connecting via integrated PuTTY to {Hostname}:{Port}", hostname, port);

        try
        {
            // Check if DLL exists
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PairAdminPuTTY.dll");
            TerminalDebugLog.Log($"DLL path: {dllPath}");
            TerminalDebugLog.Log($"DLL exists: {File.Exists(dllPath)}");

            if (!File.Exists(dllPath))
            {
                TerminalDebugLog.Log("ERROR: DLL not found");
                MessageBox.Show(
                    "Integrated PuTTY library (PairAdminPuTTY.dll) not found.\n\n" +
                    "Please reinstall PairAdmin or switch to External PuTTY mode.",
                    "Integration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Register callback for terminal I/O
            TerminalDebugLog.Log("Registering callback");
            _callbackDelegate = OnPuTTYCallback;
            PuTTYInterop.RegisterCallback(_callbackDelegate);

            // Get the WPF window handle for embedding
            var wpfHandle = GetWpfWindowHandle();
            TerminalDebugLog.Log($"WPF window handle: {wpfHandle}");

            // Initialize integrated PuTTY with parent window for embedding
            TerminalDebugLog.Log("Calling PuTTYInterop.Initialize()");
            _logger.LogDebug("Initializing integrated PuTTY with parent HWND: {Handle}", wpfHandle);
            if (!PuTTYInterop.Initialize(wpfHandle))
            {
                var errorMsg = PuTTYInterop.GetErrorMessage() ?? "Unknown error";
                TerminalDebugLog.Log($"ERROR: Initialize failed: {errorMsg}");
                TerminalDebugLog.Log($"DLL log path: {PuTTYInterop.GetDllLogPath()}");
                _logger.LogWarning("Integrated PuTTY initialization failed: {Error}", errorMsg);
                MessageBox.Show(
                    $"Integrated PuTTY initialization failed:\n{errorMsg}\n\n" +
                    $"Check log file: {PuTTYInterop.GetDllLogPath()}",
                    "Integration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            TerminalDebugLog.Log($"Initialize succeeded, state: {PuTTYInterop.State}");
            _logger.LogDebug("PuTTY initialized, state: {State}", PuTTYInterop.State);

            // Connect via integrated PuTTY DLL
            TerminalDebugLog.Log("Calling PuTTYInterop.Connect()");
            bool success = PuTTYInterop.Connect(hostname, port, username);
            TerminalDebugLog.Log($"Connect returned: {success}, state: {PuTTYInterop.State}");
            if (!success)
            {
                var errorMsg = PuTTYInterop.GetErrorMessage() ?? "Connection failed";
                TerminalDebugLog.Log($"ERROR: Connect failed: {errorMsg}");
                _logger.LogWarning("Integrated PuTTY connection failed: {Error}", errorMsg);
                MessageBox.Show(
                    $"Connection failed:\n{errorMsg}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get the terminal window handle from integrated PuTTY
            _puttyWindowHandle = PuTTYInterop.GetTerminalHandle();
            TerminalDebugLog.Log($"Terminal HWND: {_puttyWindowHandle}, state: {PuTTYInterop.State}");
            _logger.LogDebug("Got terminal HWND: {Handle}, state: {State}", _puttyWindowHandle, PuTTYInterop.State);

            if (_puttyWindowHandle != IntPtr.Zero)
            {
                // Embed the PuTTY window into our WPF container
                TerminalDebugLog.Log("Calling EmbedPuTTYWindow()");
                EmbedPuTTYWindow();
                _isConnected = true;
                UpdateConnectionUI(true, hostname);
                Connected?.Invoke(this, EventArgs.Empty);
                TerminalDebugLog.Log("=== ConnectIntegrated SUCCESS ===");
                _logger.LogInformation("Connected to {Hostname} via integrated PuTTY", hostname);
            }
            else
            {
                TerminalDebugLog.Log("ERROR: Terminal HWND is zero");
                _logger.LogWarning("Integrated PuTTY window handle not available");
                MessageBox.Show(
                    "Connection established but terminal window not available.\n" +
                    $"Check log: {PuTTYInterop.GetDllLogPath()}",
                    "Connection Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogError(ex, "PairAdminPuTTY.dll not found");
            MessageBox.Show(
                "PairAdminPuTTY.dll could not be loaded.\n\n" +
                "Please ensure the DLL is present in the application directory.",
                "DLL Load Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect via integrated PuTTY");
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPuTTYCallback(PuTTYInterop.PairAdminEventType eventType, IntPtr data, int length)
    {
        try
        {
            TerminalDebugLog.Log($"OnPuTTYCallback: eventType={eventType} data={data} length={length}");

            // Handle connection state events (no data)
            if (eventType == PuTTYInterop.PairAdminEventType.Connected)
            {
                TerminalDebugLog.Log("Callback: Connected event");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _logger.LogInformation("PuTTY callback: Connected");
                    _isConnected = true;
                    Connected?.Invoke(this, EventArgs.Empty);
                }));
                return;
            }

            if (eventType == PuTTYInterop.PairAdminEventType.Disconnected)
            {
                TerminalDebugLog.Log("Callback: Disconnected event");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _logger.LogInformation("PuTTY callback: Disconnected");
                    _isConnected = false;
                    UpdateConnectionUI(false);
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }));
                return;
            }

            if (eventType == PuTTYInterop.PairAdminEventType.Error)
            {
                string errorMsg = "Unknown error";
                if (length > 0 && data != IntPtr.Zero)
                {
                    byte[] buffer = new byte[length];
                    Marshal.Copy(data, buffer, 0, length);
                    errorMsg = Encoding.UTF8.GetString(buffer);
                }
                TerminalDebugLog.Log($"Callback: Error event - {errorMsg}");
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _logger.LogError("PuTTY callback: Error - {Message}", errorMsg);
                    MessageBox.Show($"Terminal error: {errorMsg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
                return;
            }

            // Handle I/O events (require data)
            if (length <= 0 || data == IntPtr.Zero) return;

            byte[] ioBuffer = new byte[length];
            Marshal.Copy(data, ioBuffer, 0, length);
            string text = Encoding.UTF8.GetString(ioBuffer);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (eventType == PuTTYInterop.PairAdminEventType.Output)
                {
                    TerminalOutput?.Invoke(this, new TerminalOutputEventArgs(text));
                    _logger.LogTrace("Terminal output: {Length} bytes", length);
                }
                else if (eventType == PuTTYInterop.PairAdminEventType.Input)
                {
                    TerminalInput?.Invoke(this, new TerminalInputEventArgs(text));
                    _logger.LogTrace("Terminal input: {Length} bytes", length);
                }
            }));
        }
        catch (Exception ex)
        {
            TerminalDebugLog.LogException(ex, "OnPuTTYCallback");
            _logger.LogError(ex, "Error processing PuTTY callback");
        }
    }

    #endregion

    #region External Mode (putty.exe)

    private void ConnectExternal(string hostname, int port, string username)
    {
        TerminalDebugLog.Log("=== ConnectExternal START ===");
        TerminalDebugLog.Log($"hostname={hostname} port={port} username={username}");

        if (_isConnected) Disconnect();
        _logger.LogInformation("Connecting via external PuTTY to {Hostname}:{Port}", hostname, port);

        try
        {
            // Find PuTTY executable
            TerminalDebugLog.Log($"ExternalPuTTYPath from settings: {_settings.ExternalPuTTYPath}");
            var puttyPath = TerminalSettings.FindExternalPuTTY(_settings.ExternalPuTTYPath);
            TerminalDebugLog.Log($"FindExternalPuTTY returned: {puttyPath}");

            if (string.IsNullOrEmpty(puttyPath))
            {
                TerminalDebugLog.Log("ERROR: PuTTY executable not found");
                var result = MessageBox.Show(
                    "PuTTY executable not found.\n\n" +
                    "Would you like to:\n" +
                    "- Install PuTTY from https://www.putty.org\n" +
                    "- Or switch to Integrated mode\n\n" +
                    "Open PuTTY download page?",
                    "PuTTY Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.putty.org",
                        UseShellExecute = true
                    });
                }
                return;
            }

            // Build command line arguments
            var args = new StringBuilder("-ssh ");
            if (!string.IsNullOrEmpty(username))
                args.Append($"-l {username} ");
            args.Append($"-P {port} {hostname}");

            TerminalDebugLog.Log($"Launching PuTTY: {puttyPath} {args}");
            _logger.LogDebug("Launching PuTTY: {Path} {Args}", puttyPath, args);

            _puttyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = puttyPath,
                Arguments = args.ToString(),
                UseShellExecute = false
            });

            TerminalDebugLog.Log($"Process started, ID: {_puttyProcess?.Id}");
            TerminalDebugLog.Log("Waiting for input idle...");
            _puttyProcess?.WaitForInputIdle(5000);
            _puttyWindowHandle = _puttyProcess?.MainWindowHandle ?? IntPtr.Zero;
            TerminalDebugLog.Log($"MainWindowHandle: {_puttyWindowHandle}");

            if (_puttyWindowHandle != IntPtr.Zero)
            {
                TerminalDebugLog.Log("Calling EmbedPuTTYWindow()");
                EmbedPuTTYWindow();
                _isConnected = true;
                UpdateConnectionUI(true, hostname);
                Connected?.Invoke(this, EventArgs.Empty);
                TerminalDebugLog.Log("=== ConnectExternal SUCCESS ===");
                _logger.LogInformation("Connected to {Hostname} via external PuTTY", hostname);
            }
            else
            {
                TerminalDebugLog.Log("WARNING: Window handle is zero");
                MessageBox.Show(
                    "PuTTY started but window handle not available.\n" +
                    "The terminal may have opened in a separate window.",
                    "Connection Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            TerminalDebugLog.LogException(ex, "ConnectExternal");
            _logger.LogError(ex, "Failed to connect via external PuTTY");
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Window Embedding

    private void EmbedPuTTYWindow()
    {
        TerminalDebugLog.Log("=== EmbedPuTTYWindow START ===");
        TerminalDebugLog.Log($"_puttyWindowHandle: {_puttyWindowHandle}");

        if (_puttyWindowHandle == IntPtr.Zero)
        {
            TerminalDebugLog.Log("ERROR: _puttyWindowHandle is zero, returning");
            return;
        }

        var wpfHandle = GetWpfWindowHandle();
        TerminalDebugLog.Log($"WPF handle: {wpfHandle}");

        if (wpfHandle == IntPtr.Zero)
        {
            TerminalDebugLog.Log("ERROR: WPF handle is zero, returning");
            return;
        }

        try
        {
            // Remove window chrome and set as child
            TerminalDebugLog.Log("Getting window style...");
            int style = NativeMethods.GetWindowLong(_puttyWindowHandle, NativeMethods.GWL_STYLE);
            TerminalDebugLog.Log($"Original style: 0x{style:X8}");

            style = (style & ~NativeMethods.WS_CAPTION) | NativeMethods.WS_CHILD;
            TerminalDebugLog.Log($"New style: 0x{style:X8}");

            NativeMethods.SetWindowLong(_puttyWindowHandle, NativeMethods.GWL_STYLE, style);
            TerminalDebugLog.Log("SetWindowLong completed");

            // Set parent
            TerminalDebugLog.Log("Calling SetParentWindow...");
            PuTTYInterop.SetParentWindow(_puttyWindowHandle, wpfHandle);
            TerminalDebugLog.Log("SetParentWindow completed");

            // Resize to fit
            TerminalDebugLog.Log("Calling ResizeTerminalToFit...");
            ResizeTerminalToFit();
            TerminalDebugLog.Log("ResizeTerminalToFit completed");

            // Show window
            TerminalDebugLog.Log("Calling Show...");
            PuTTYInterop.Show(_puttyWindowHandle);
            TerminalDebugLog.Log("Show completed");

            TerminalDebugLog.Log("=== EmbedPuTTYWindow SUCCESS ===");
            _logger.LogDebug("PuTTY window embedded successfully");
        }
        catch (Exception ex)
        {
            TerminalDebugLog.LogException(ex, "EmbedPuTTYWindow");
            throw;
        }
    }

    private IntPtr GetWpfWindowHandle()
    {
        var window = Window.GetWindow(this);
        return window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;
    }

    private void ResizeTerminalToFit()
    {
        if (_puttyWindowHandle == IntPtr.Zero) return;

        var contentArea = TerminalContentArea;
        if (contentArea == null) return;

        var point = contentArea.TransformToAncestor(Window.GetWindow(this)).Transform(new Point(0, 0));
        PuTTYInterop.ResizeChildWindow(
            _puttyWindowHandle,
            (int)point.X,
            (int)point.Y,
            (int)contentArea.ActualWidth,
            (int)contentArea.ActualHeight);
    }

    #endregion

    #region Connection Management

    private void UpdateConnectionUI(bool connected, string hostname = "")
    {
        Dispatcher.Invoke(() =>
        {
            if (connected)
            {
                ConnectionStatusText.Text = $" - Connected to {hostname}";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(39, 174, 96));
                ConnectionBar.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Visible;
                PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ConnectionStatusText.Text = " - Disconnected";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(128, 128, 128));
                ConnectionBar.Visibility = Visibility.Visible;
                DisconnectButton.Visibility = Visibility.Collapsed;
                PlaceholderPanel.Visibility = Visibility.Visible;
            }
        });
    }

    public void Disconnect()
    {
        TerminalDebugLog.Log("=== Disconnect START ===");
        TerminalDebugLog.Log($"Mode: {_settings.Mode}, _puttyProcess: {(_puttyProcess != null ? "non-null" : "null")}");

        try
        {
            // Disconnect integrated PuTTY if in that mode
            if (_settings.Mode == TerminalMode.Integrated && _puttyProcess == null)
            {
                try
                {
                    TerminalDebugLog.Log("Calling PuTTYInterop.Disconnect()...");
                    // First disconnect the session
                    PuTTYInterop.Disconnect();
                    TerminalDebugLog.Log("PuTTYInterop.Disconnect() completed");
                    _logger.LogDebug("PuTTY session disconnected");
                }
                catch (Exception ex)
                {
                    TerminalDebugLog.LogException(ex, "PuTTYInterop.Disconnect");
                    _logger.LogWarning(ex, "Error disconnecting integrated PuTTY");
                }
            }

            // Disconnect external PuTTY process
            if (_puttyProcess != null && !_puttyProcess.HasExited)
            {
                TerminalDebugLog.Log("Closing external PuTTY process...");
                _puttyProcess.CloseMainWindow();
                if (!_puttyProcess.WaitForExit(2000))
                {
                    TerminalDebugLog.Log("Process didn't exit, killing...");
                    _puttyProcess.Kill();
                }
                _puttyProcess.Dispose();
                TerminalDebugLog.Log("External PuTTY process closed");
            }
            _puttyProcess = null;
            _puttyWindowHandle = IntPtr.Zero;
            _isConnected = false;
            _callbackDelegate = null;
            UpdateConnectionUI(false);
            Disconnected?.Invoke(this, EventArgs.Empty);
            TerminalDebugLog.Log("=== Disconnect SUCCESS ===");
            _logger.LogInformation("Disconnected from terminal");
        }
        catch (Exception ex)
        {
            TerminalDebugLog.LogException(ex, "Disconnect");
            _logger.LogError(ex, "Error disconnecting");
        }
    }

    /// <summary>
    /// Fully shutdown the integrated PuTTY system (cleanup thread and resources)
    /// </summary>
    public void ShutdownIntegrated()
    {
        try
        {
            if (_settings.Mode == TerminalMode.Integrated)
            {
                PuTTYInterop.Shutdown();
                _logger.LogInformation("Integrated PuTTY shutdown complete");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error shutting down integrated PuTTY");
        }
    }

    public void SendCommand(string command)
    {
        if (string.IsNullOrEmpty(command) || _puttyWindowHandle == IntPtr.Zero) return;

        NativeMethods.SetForegroundWindow(_puttyWindowHandle);
        foreach (char c in command)
            NativeMethods.SendMessage(_puttyWindowHandle, NativeMethods.WM_CHAR, (IntPtr)c, IntPtr.Zero);
        NativeMethods.SendMessage(_puttyWindowHandle, NativeMethods.WM_CHAR, (IntPtr)13, IntPtr.Zero); // Enter
    }

    #endregion

    #region Lifecycle

    private void OnLoaded(object s, RoutedEventArgs e) => InitializeTerminal();
    private void OnUnloaded(object s, RoutedEventArgs e) => Dispose();
    private void OnSizeChanged(object s, SizeChangedEventArgs e) => ResizeTerminalToFit();

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            ShutdownIntegrated();
            _disposed = true;
        }
    }

    #endregion

    #region Native Methods

    private static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_CHILD = 0x40000000;
        public const uint WM_CHAR = 0x0102;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }

    #endregion
}

/// <summary>
/// Event args for terminal output
/// </summary>
public class TerminalOutputEventArgs : EventArgs
{
    public string Text { get; }
    public TerminalOutputEventArgs(string text) => Text = text;
}

/// <summary>
/// Event args for terminal input
/// </summary>
public class TerminalInputEventArgs : EventArgs
{
    public string Text { get; }
    public TerminalInputEventArgs(string text) => Text = text;
}
