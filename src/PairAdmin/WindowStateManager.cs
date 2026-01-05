using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PairAdmin;

/// <summary>
/// Window state manager for persisting and restoring window configuration
/// </summary>
public class WindowStateManager
{
    private readonly ILogger<WindowStateManager> _logger;
    private readonly WindowStateOptions _options;

    /// <summary>
    /// Initializes a new instance of WindowStateManager class
    /// </summary>
    public WindowStateManager(
        ILogger<WindowStateManager> logger,
        IOptions<WindowStateOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Save current window state
    /// </summary>
    public void SaveState(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        // Only save if window is in normal state
        if (window.WindowState != System.Windows.WindowState.Normal)
        {
            _logger.LogDebug("Skipping state save: window not in normal state");
            return;
        }

        var state = new WindowState
        {
            Width = window.ActualWidth,
            Height = window.ActualHeight,
            Left = window.Left,
            Top = window.Top,
            IsMaximized = false
        };

        // TODO: Persist to application settings
        // _options.WindowState = state;
        
        _logger.LogDebug("Saved window state: {Width}x{Height} at ({Left}, {Top})", 
            state.Width, state.Height, state.Left, state.Top);
    }

    /// <summary>
    /// Restore window state
    /// </summary>
    public void RestoreState(Window window)
    {
        if (window == null)
            throw new ArgumentNullException(nameof(window));

        // TODO: Load from application settings
        // var state = _options.WindowState;
        
        _logger.LogInformation("Restoring window state from saved configuration");
        
        // For now, use default values
        // In production, this would load saved state from configuration
        
        _logger.LogDebug("Window state restore: Using defaults (400x800 at center)");
    }
}

/// <summary>
/// Window state configuration options
/// </summary>
public class WindowStateOptions
{
    public WindowState? WindowState { get; set; }
}
