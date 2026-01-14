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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = TerminalSettings.Load();
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void InitializeTerminal()
    {
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

    #region Integrated Mode (PairAdminPuTTY.dll)

    private void ConnectIntegrated(string hostname, int port, string username)
    {
        if (_isConnected) Disconnect();
        _logger.LogInformation("Connecting via integrated PuTTY to {Hostname}:{Port}", hostname, port);

        try
        {
            // Check if DLL exists
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PairAdminPuTTY.dll");
            if (!File.Exists(dllPath))
            {
                MessageBox.Show(
                    "Integrated PuTTY library (PairAdminPuTTY.dll) not found.\n\n" +
                    "Please reinstall PairAdmin or switch to External PuTTY mode.",
                    "Integration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Register callback for terminal I/O
            _callbackDelegate = OnPuTTYCallback;
            PuTTYInterop.RegisterCallback(_callbackDelegate);

            // Initialize integrated PuTTY (required before connect)
            var wpfHandle = GetWpfWindowHandle();
            if (!PuTTYInterop.Initialize(wpfHandle))
            {
                _logger.LogWarning("Integrated PuTTY initialization failed, falling back to external");
                MessageBox.Show(
                    "Integrated PuTTY initialization failed.\n\n" +
                    "This is expected - full DLL integration is still in development.\n" +
                    "Falling back to External PuTTY mode.",
                    "Integration Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                ConnectExternal(hostname, port, username);
                return;
            }

            // Connect via integrated PuTTY DLL
            bool success = PuTTYInterop.Connect(hostname, port, username);
            if (!success)
            {
                _logger.LogWarning("Integrated PuTTY connection failed, falling back to external");
                MessageBox.Show(
                    "Integrated PuTTY connection could not be established.\n\n" +
                    "Falling back to External PuTTY mode.",
                    "Connection Fallback",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                ConnectExternal(hostname, port, username);
                return;
            }

            // Get the terminal window handle from integrated PuTTY
            _puttyWindowHandle = PuTTYInterop.GetTerminalHandle();
            if (_puttyWindowHandle != IntPtr.Zero)
            {
                EmbedPuTTYWindow();
                _isConnected = true;
                UpdateConnectionUI(true, hostname);
                Connected?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Connected to {Hostname} via integrated PuTTY", hostname);
            }
            else
            {
                _logger.LogWarning("Integrated PuTTY window handle not available");
                MessageBox.Show(
                    "Connection initiated but terminal window not available.\n\n" +
                    "Falling back to External PuTTY mode.",
                    "Connection Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                PuTTYInterop.Disconnect();
                ConnectExternal(hostname, port, username);
            }
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogError(ex, "PairAdminPuTTY.dll not found");
            MessageBox.Show(
                "PairAdminPuTTY.dll could not be loaded.\n\n" +
                "Falling back to External PuTTY mode.",
                "DLL Load Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            ConnectExternal(hostname, port, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect via integrated PuTTY");
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPuTTYCallback(PuTTYInterop.PairAdminEventType eventType, IntPtr data, int length)
    {
        if (length <= 0 || data == IntPtr.Zero) return;

        try
        {
            byte[] buffer = new byte[length];
            Marshal.Copy(data, buffer, 0, length);
            string text = Encoding.UTF8.GetString(buffer);

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
            _logger.LogError(ex, "Error processing PuTTY callback");
        }
    }

    #endregion

    #region External Mode (putty.exe)

    private void ConnectExternal(string hostname, int port, string username)
    {
        if (_isConnected) Disconnect();
        _logger.LogInformation("Connecting via external PuTTY to {Hostname}:{Port}", hostname, port);

        try
        {
            // Find PuTTY executable
            var puttyPath = TerminalSettings.FindExternalPuTTY(_settings.ExternalPuTTYPath);
            if (string.IsNullOrEmpty(puttyPath))
            {
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

            _logger.LogDebug("Launching PuTTY: {Path} {Args}", puttyPath, args);

            _puttyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = puttyPath,
                Arguments = args.ToString(),
                UseShellExecute = false
            });

            _puttyProcess?.WaitForInputIdle(5000);
            _puttyWindowHandle = _puttyProcess?.MainWindowHandle ?? IntPtr.Zero;

            if (_puttyWindowHandle != IntPtr.Zero)
            {
                EmbedPuTTYWindow();
                _isConnected = true;
                UpdateConnectionUI(true, hostname);
                Connected?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Connected to {Hostname} via external PuTTY", hostname);
            }
            else
            {
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
            _logger.LogError(ex, "Failed to connect via external PuTTY");
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Window Embedding

    private void EmbedPuTTYWindow()
    {
        if (_puttyWindowHandle == IntPtr.Zero) return;

        var wpfHandle = GetWpfWindowHandle();
        if (wpfHandle == IntPtr.Zero) return;

        // Remove window chrome and set as child
        int style = NativeMethods.GetWindowLong(_puttyWindowHandle, NativeMethods.GWL_STYLE);
        style = (style & ~NativeMethods.WS_CAPTION) | NativeMethods.WS_CHILD;
        NativeMethods.SetWindowLong(_puttyWindowHandle, NativeMethods.GWL_STYLE, style);

        // Set parent
        PuTTYInterop.SetParentWindow(_puttyWindowHandle, wpfHandle);

        // Resize to fit
        ResizeTerminalToFit();

        // Show window
        PuTTYInterop.Show(_puttyWindowHandle);

        _logger.LogDebug("PuTTY window embedded successfully");
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
        try
        {
            // Disconnect integrated PuTTY if in that mode
            if (_settings.Mode == TerminalMode.Integrated && _puttyProcess == null)
            {
                try
                {
                    PuTTYInterop.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disconnecting integrated PuTTY");
                }
            }

            // Disconnect external PuTTY process
            if (_puttyProcess != null && !_puttyProcess.HasExited)
            {
                _puttyProcess.CloseMainWindow();
                if (!_puttyProcess.WaitForExit(2000))
                    _puttyProcess.Kill();
                _puttyProcess.Dispose();
            }
            _puttyProcess = null;
            _puttyWindowHandle = IntPtr.Zero;
            _isConnected = false;
            _callbackDelegate = null;
            UpdateConnectionUI(false);
            Disconnected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Disconnected from terminal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting");
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
