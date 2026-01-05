using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PairAdmin;

/// <summary>
/// Main window for PairAdmin application
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly WindowStateManager _windowStateManager;
    private WindowState? _lastWindowState;

    /// <summary>
    /// Initializes a new instance of MainWindow class
    /// </summary>
    public MainWindow(ILogger<MainWindow> logger, WindowStateManager windowStateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _windowStateManager = windowStateManager ?? throw new ArgumentNullException(nameof(windowStateManager));
        InitializeComponent();
        
        // Subscribe to window events
        this.StateChanged += MainWindow_StateChanged;
        this.Closing += MainWindow_Closing;
        
        // Restore window state if available
        RestoreWindowState();
    }

    /// <summary>
    /// Window state changed event handler
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
{
        _logger.LogDebug("Window state changed: {WindowState}", this.WindowState);
        
        // Save window state when changed (but not while maximized)
        if (this.WindowState == WindowState.Normal)
        {
            SaveWindowState();
        }
}

    /// <summary>
    /// Window closing event handler
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
{
        _logger.LogInformation("MainWindow closing");
        
        // Save window state before closing
        SaveWindowState();
}

    /// <summary>
    /// Save current window state
    /// </summary>
    private void SaveWindowState()
    {
        _windowStateManager.SaveState(this);
    }

    /// <summary>
    /// Restore window state from saved state
    /// </summary>
    private void RestoreWindowState()
    {
        _windowStateManager.RestoreState(this);
    }

        var state = new WindowState
        {
            Width = this.Width,
            Height = this.Height,
            Left = this.Left,
            Top = this.Top,
            IsMaximized = false
        };

        _lastWindowState = state;
        
        // TODO: Persist to application settings in future task
        _logger.LogDebug("Saved window state: {Width}x{Height} at ({Left}, {Top})", 
            state.Width, state.Height, state.Left, state.Top);
}

    /// <summary>
    /// Restore window state from saved state
    /// </summary>
    private void RestoreWindowState()
{
        if (_lastWindowState != null)
        {
            var state = _lastWindowState;
            
            // Restore position and size
            this.Left = state.Left;
            this.Top = state.Top;
            this.Width = state.Width;
            this.Height = state.Height;
            
            // Restore maximized state if needed
            if (state.IsMaximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            
            _logger.LogInformation("Restored window state: {Width}x{Height}", 
                state.Width, state.Height);
        }
}

    /// <summary>
    /// Notify property changed for window properties
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Minimize button click handler
    /// </summary>
    private void MinimizeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _logger.LogDebug("Minimize button clicked");
        this.WindowState = System.Windows.WindowState.Minimized;
    }

    /// <summary>
    /// Maximize/restore button click handler
    /// </summary>
    private void MaximizeButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _logger.LogDebug("Maximize/restore button clicked");
        
        if (this.WindowState == System.Windows.WindowState.Maximized)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }
        else
        {
            this.WindowState = System.Windows.WindowState.Maximized;
        }
    }

    /// <summary>
    /// Close button click handler
    /// </summary>
    private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _logger.LogDebug("Close button clicked");
        this.Close();
    }

    /// <summary>
    /// Divider mouse left button down handler (for resizing)
    /// </summary>
    private void Divider_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _logger.LogDebug("Divider drag started");
        // TODO: Implement drag resizing logic in Task 10.1
        // This is a placeholder for now
    }
}
