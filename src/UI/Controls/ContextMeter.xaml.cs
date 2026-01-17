using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using PairAdmin.LLMGateway;

namespace PairAdmin.UI.Controls;

/// <summary>
/// Context meter state
/// </summary>
public class ContextMeterState : INotifyPropertyChanged
{
    private int _currentTokens;
    private int _maxTokens;
    private double _percentage;

    public int CurrentTokens
    {
        get => _currentTokens;
        set
        {
            if (_currentTokens != value)
            {
                _currentTokens = value;
                OnPropertyChanged(nameof(CurrentTokens));
                OnPropertyChanged(nameof(Percentage));
            }
        }
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set
        {
            if (_maxTokens != value)
            {
                _maxTokens = value;
                OnPropertyChanged(nameof(MaxTokens));
                OnPropertyChanged(nameof(AvailableTokens));
                OnPropertyChanged(nameof(Percentage));
            }
        }
    }

    public double Percentage
    {
        get => _percentage;
        internal set
        {
            _percentage = value;
            OnPropertyChanged(nameof(Percentage));
            OnPropertyChanged(nameof(FormattedPercentage));
            OnPropertyChanged(nameof(FillColor));
        }
    }

    public int AvailableTokens => Math.Max(0, MaxTokens - CurrentTokens);

    public string FormattedPercentage => Percentage.ToString("P1");

    public string FormattedTokenCount => $"{CurrentTokens:N0} / {MaxTokens:N0}";

    public string FormattedMaxTokens => MaxTokens.ToString("N0");

    public string StatusText => Percentage switch
    {
        >= 1.0 => "OVER LIMIT",
        >= 0.90 => "CRITICAL",
        >= 0.70 => "WARNING",
        _ => "SAFE"
    };

    public MeterStatus Status => Percentage switch
    {
        >= 1.0 => MeterStatus.OverLimit,
        >= 0.90 => MeterStatus.Critical,
        >= 0.70 => MeterStatus.Warning,
        _ => MeterStatus.Safe
    };

    public Brush FillColor => Status switch
    {
        MeterStatus.Safe => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
        MeterStatus.Warning => new SolidColorBrush(Color.FromRgb(241, 196, 15)),
        MeterStatus.Critical => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
        MeterStatus.OverLimit => new SolidColorBrush(Color.FromRgb(192, 57, 43))
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Meter status
/// </summary>
public enum MeterStatus
{
    Safe,
    Warning,
    Critical,
    OverLimit
}

/// <summary>
/// Main context meter control
/// </summary>
public partial class ContextMeter : UserControl
{
    private readonly ILogger<ContextMeter> _logger;
    private ContextMeterState _state;
    private string _fillColor;

    /// <summary>
    /// Current meter state
    /// </summary>
    public ContextMeterState State => _state;

    /// <summary>
    /// Fill color for progress bar
    /// </summary>
    public string FillColor => _fillColor;

    /// <summary>
    /// Event raised when token count is updated
    /// </summary>
    public event EventHandler<TokenCountResult>? TokenCountUpdated;

    /// <summary>
    /// Initializes a new instance of ContextMeter
    /// </summary>
    public ContextMeter(ILogger<ContextMeter> logger)
    {
        InitializeComponent();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _state = new ContextMeterState();
        _fillColor = "#2ECC71";
        DataContext = _state;

        _logger.LogInformation("ContextMeter initialized");
    }

    /// <summary>
    /// Updates the meter with current token count
    /// </summary>
    /// <param name="tokens">Current token count</param>
    /// <param name="maxTokens">Maximum tokens allowed</param>
    public void UpdateTokens(int tokens, int maxTokens)
    {
        _state.CurrentTokens = tokens;
        _state.MaxTokens = maxTokens;
        _state.Percentage = maxTokens > 0 ? (double)tokens / maxTokens : 0.0;
        _fillColor = GetColorHex(_state.FillColor);

        TokenCountUpdated?.Invoke(this, new TokenCountResult
        {
            CurrentTokens = tokens,
            MaxTokens = maxTokens,
            Percentage = _state.Percentage
        });

        _logger.LogTrace($"Updated meter: {tokens}/{maxTokens} ({_state.Percentage:P1})");
    }

    /// <summary>
    /// Resets the meter to zero
    /// </summary>
    public void Reset()
    {
        _state.CurrentTokens = 0;
        _state.MaxTokens = 1000;
        _state.Percentage = 0.0;
        _fillColor = GetColorHex(_state.FillColor);

        _logger.LogInformation("Meter reset to zero");
    }

    /// <summary>
    /// Gets the color hex code from brush
    /// </summary>
    private string GetColorHex(Brush brush)
    {
        if (brush is SolidColorBrush solidBrush)
        {
            var color = solidBrush.Color;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return "#2ECC71";
    }

    /// <summary>
    /// Token count result for events
    /// </summary>
    public class TokenCountResult
    {
        public int CurrentTokens { get; set; }
        public int MaxTokens { get; set; }
        public double Percentage { get; set; }
    }
}
