using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PairAdmin.UI.Controls;

public partial class TerminalPane : UserControl, IDisposable
{
    private readonly ILogger<TerminalPane> _logger;
    private Process? _puttyProcess;
    private bool _isConnected;
    private bool _disposed;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public bool IsConnected => _isConnected;

    public TerminalPane() : this(NullLogger<TerminalPane>.Instance) { }

    public TerminalPane(ILogger<TerminalPane> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeComponent();
    }

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
        ConnectExternal(hostname, port, username);
    }

    private void ConnectExternal(string hostname, int port, string username)
    {
        if (_isConnected) Disconnect();
        
        try
        {
            var puttyPath = FindPuTTYExecutable();
            if (string.IsNullOrEmpty(puttyPath))
            {
                ShowPuTTYNotFoundError();
                return;
            }
            
            _logger.LogInformation("Found PuTTY at: {PuttyPath}", puttyPath);
            
            var args = new StringBuilder("-ssh ");
            if (!string.IsNullOrEmpty(username))
                args.Append($"-l {username} ");
            args.Append($"-P {port} {hostname}");
            
            _logger.LogInformation("Launching PuTTY: {CommandLine}", puttyPath + " " + args);
            
            _puttyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = puttyPath,
                Arguments = args.ToString(),
                UseShellExecute = false
            });
            
            _logger.LogInformation("Process started, ID: {ProcessId}", _puttyProcess?.Id ?? -1);
            
            _logger.LogInformation("Waiting for input idle...");
            var waitResult = _puttyProcess?.WaitForInputIdle(5000);
            _logger.LogInformation("WaitForInputIdle result: {Result}", waitResult);
            
            var windowHandle = _puttyProcess?.MainWindowHandle ?? IntPtr.Zero;
            _logger.LogInformation("MainWindowHandle: {Handle} (0x{HandleHex})", windowHandle, windowHandle.ToInt64().ToString("X8"));
            
            if (windowHandle != IntPtr.Zero)
            {
                var windowInfo = GetWindowInfo(windowHandle);
                _logger.LogInformation("Main Window Info: {WindowInfo}", windowInfo);
                NativeMethods.SetForegroundWindow(windowHandle);
                _logger.LogInformation("Foreground set successfully");
            }
            else
            {
                _logger.LogWarning("MainWindowHandle is null/zero - searching for PuTTY windows...");
                var allWindows = GetAllProcessWindows(_puttyProcess?.Id ?? -1);
                _logger.LogInformation("Found {Count} windows for process {ProcessId}: {Windows}", 
                    allWindows.Count, _puttyProcess?.Id ?? -1, string.Join("; ", allWindows));
                
                if (allWindows.Count > 0)
                {
                    var firstWindow = allWindows[0];
                    _logger.LogInformation("Using first found window: {Handle} - {Title}", 
                        firstWindow.Handle, firstWindow.Title);
                }
            }
            
            _isConnected = true;
            ConnectionStatusText.Text = $" - Connected to {hostname}";
            ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(39, 174, 96));
            Connected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("=== ConnectExternal SUCCESS ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect via external PuTTY");
            _logger.LogError("Error details: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private (IntPtr Handle, string Title) GetWindowInfo(IntPtr hWnd)
    {
        try
        {
            var title = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, title, 256);
            var className = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, 256);
            NativeMethods.GetWindowRect(hWnd, out var rect);
            
            _logger.LogInformation("  Window 0x{HandleHex}: Title='{Title}' Class='{Class}' Rect=({Left},{Top},{Right},{Bottom})",
                hWnd.ToInt64().ToString("X8"), title.ToString(), className.ToString(),
                rect.Left, rect.Top, rect.Right, rect.Bottom);
            
            return (hWnd, title.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting window info for 0x{HandleHex}", hWnd.ToInt64().ToString("X8"));
            return (hWnd, $"ERROR: {ex.Message}");
        }
    }

    private List<(IntPtr Handle, string Title)> GetAllProcessWindows(int processId)
    {
        var windows = new List<(IntPtr, string)>();
        
        if (processId <= 0)
        {
            _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
            return windows;
        }
        
        _logger.LogInformation("Enumerating all windows for process {ProcessId}...", processId);
        
        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            try
            {
                int windowPid;
                NativeMethods.GetWindowThreadProcessId(hWnd, out windowPid);
                
                if (windowPid == processId)
                {
                    var title = new StringBuilder(256);
                    NativeMethods.GetWindowText(hWnd, title, 256);
                    
                    var className = new StringBuilder(256);
                    NativeMethods.GetClassName(hWnd, className, 256);
                    
                    NativeMethods.GetWindowRect(hWnd, out var rect);
                    var isVisible = NativeMethods.IsWindowVisible(hWnd);
                    
                    _logger.LogInformation("  Found window: 0x{HandleHex} Title='{Title}' Class='{Class}' Visible={Visible} Rect=({Left},{Top},{Right},{Bottom})",
                        hWnd.ToInt64().ToString("X8"), title.ToString(), className.ToString(), isVisible,
                        rect.Left, rect.Top, rect.Right, rect.Bottom);
                    
                    windows.Add((hWnd, title.ToString()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating window 0x{HandleHex}", hWnd.ToInt64().ToString("X8"));
            }
            
            return true; // Continue enumeration
        }, IntPtr.Zero);
        
        _logger.LogInformation("Total windows found: {Count}", windows.Count);
        return windows;
    }

    private string FindPuTTYExecutable()
    {
        var paths = new[] { Environment.GetEnvironmentVariable("PATH") ?? "",
                          Environment.ExpandEnvironmentVariables("%ProgramFiles%PuTTY"),
                          @"C:\Program Files\PuTTY", @"C:\Program Files (x86)\PuTTY" };
        
        foreach (var dir in paths)
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var path = Path.Combine(dir, "putty.exe");
            if (File.Exists(path)) return path;
            path = Path.Combine(dir, "putty64.exe");
            if (File.Exists(path)) return path;
        }
        return "";
    }

    private void ShowPuTTYNotFoundError()
    {
        var result = MessageBox.Show(
            "PuTTY executable not found.\n\nTo install PuTTY:\n1. Download from: https://www.putty.org\n2. Extract to a folder in your PATH\n3. Restart PairAdmin",
            "PuTTY Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo { FileName = "https://www.putty.org", UseShellExecute = true });
        }
    }

    public void Disconnect()
    {
        try
        {
            if (_puttyProcess != null && !_puttyProcess.HasExited)
            {
                _puttyProcess.CloseMainWindow();
                if (!_puttyProcess.WaitForExit(2000))
                    _puttyProcess.Kill();
                _puttyProcess.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect");
        }
        
        _puttyProcess = null;
        _isConnected = false;
        ConnectionStatusText.Text = " - Disconnected";
        ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(128, 128, 128));
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
