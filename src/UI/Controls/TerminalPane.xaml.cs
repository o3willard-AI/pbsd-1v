using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PairAdmin.Interop;

namespace PairAdmin.UI.Controls;

public partial class TerminalPane : UserControl, IDisposable
{
    private readonly ILogger<TerminalPane> _logger;
    private IntPtr _puttyWindowHandle = IntPtr.Zero;
    private Process? _puttyProcess;
    private bool _isConnected;
    private bool _disposed;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public bool IsConnected => _isConnected;
    public IntPtr WindowHandle => _puttyWindowHandle;

    public TerminalPane() : this(NullLogger<TerminalPane>.Instance)
    {
    }

    public TerminalPane(ILogger<TerminalPane> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void InitializeTerminal() => _logger.LogInformation("Initializing TerminalPane");

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
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
        ConnectToPuTTY(hostname, port, username);
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        DisconnectFromPuTTY();
    }

    private void UpdateConnectionUI(bool connected, string hostname = "")
    {
        Dispatcher.Invoke(() =>
        {
            if (connected)
            {
                ConnectionStatusText.Text = $" - Connected to {hostname}";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));
                ConnectionBar.Visibility = Visibility.Collapsed;
                DisconnectButton.Visibility = Visibility.Visible;
                PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ConnectionStatusText.Text = " - Disconnected";
                ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));
                ConnectionBar.Visibility = Visibility.Visible;
                DisconnectButton.Visibility = Visibility.Collapsed;
                PlaceholderPanel.Visibility = Visibility.Visible;
            }
        });
    }

    public void ConnectToPuTTY(string hostname, int port = 22, string username = "")
    {
        if (_isConnected) DisconnectFromPuTTY();
        _logger.LogInformation("Connecting to {Hostname}:{Port}", hostname, port);
        try
        {
            var args = new StringBuilder("-ssh ");
            if (!string.IsNullOrEmpty(username)) args.Append("-l " + username + " ");
            args.Append("-P " + port + " " + hostname);

            _puttyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "putty.exe",
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
            }
            else
            {
                MessageBox.Show("Failed to start PuTTY. Make sure putty.exe is installed and in your PATH.",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect");
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EmbedPuTTYWindow()
    {
        if (_puttyWindowHandle == IntPtr.Zero) return;
        var wpfHandle = GetWpfWindowHandle();
        if (wpfHandle == IntPtr.Zero) return;

        int style = NativeMethods.GetWindowLong(_puttyWindowHandle, -16);
        style = (style & ~0x00CC0000) | 0x40000000;
        NativeMethods.SetWindowLong(_puttyWindowHandle, -16, style);
        PuTTYInterop.SetParentWindow(_puttyWindowHandle, wpfHandle);
        ResizeTerminalToFit();
        PuTTYInterop.Show(_puttyWindowHandle);
    }

    private IntPtr GetWpfWindowHandle()
    {
        var window = Window.GetWindow(this);
        return window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;
    }

    public void DisconnectFromPuTTY()
    {
        try
        {
            if (_puttyProcess != null && !_puttyProcess.HasExited)
            {
                _puttyProcess.CloseMainWindow();
                if (!_puttyProcess.WaitForExit(2000)) _puttyProcess.Kill();
                _puttyProcess.Dispose();
            }
            _puttyProcess = null;
            _puttyWindowHandle = IntPtr.Zero;
            _isConnected = false;
            UpdateConnectionUI(false);
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error disconnecting"); }
    }

    public void SendCommand(string command)
    {
        if (string.IsNullOrEmpty(command) || _puttyWindowHandle == IntPtr.Zero) return;
        NativeMethods.SetForegroundWindow(_puttyWindowHandle);
        foreach (char c in command)
            NativeMethods.SendMessage(_puttyWindowHandle, 0x0102, (IntPtr)c, IntPtr.Zero);
        NativeMethods.SendMessage(_puttyWindowHandle, 0x0102, (IntPtr)13, IntPtr.Zero);
    }

    private void ResizeTerminalToFit()
    {
        if (_puttyWindowHandle == IntPtr.Zero) return;
        PuTTYInterop.ResizeChildWindow(_puttyWindowHandle, 0, 0, (int)ActualWidth, (int)ActualHeight);
    }

    public void ResizeTerminal(double w, double h)
    {
        if (_puttyWindowHandle != IntPtr.Zero)
            PuTTYInterop.ResizeChildWindow(_puttyWindowHandle, 0, 0, (int)w, (int)h);
    }

    private void UpdateStatus(string s) { if (FindName("StatusTextBlock") is TextBlock tb) tb.Text = s; }
    public void SetPuTTYWindowHandle(IntPtr h) { _puttyWindowHandle = h; if (h != IntPtr.Zero) EmbedPuTTYWindow(); }
    private void OnLoaded(object s, RoutedEventArgs e) => InitializeTerminal();
    private void OnUnloaded(object s, RoutedEventArgs e) => Dispose();
    private void OnSizeChanged(object s, SizeChangedEventArgs e) => ResizeTerminalToFit();
    public void Dispose() { if (!_disposed) { DisconnectFromPuTTY(); _disposed = true; } }

    private static class NativeMethods
    {
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int i, int v);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    }
}
