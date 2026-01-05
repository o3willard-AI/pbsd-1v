using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using PairAdmin.Chat;

namespace PairAdmin.UI.Controls;

/// <summary>
/// Individual message control for chat
/// </summary>
public partial class ChatMessageControl : UserControl
{
    private ChatMessage? _message;
    private readonly ILogger<ChatMessageControl> _logger;

    /// <summary>
    /// Gets or sets the chat message
    /// </summary>
    public ChatMessage? Message
    {
        get => _message;
        set
        {
            _message = value;
            DataContext = value;
            UpdateAppearance();
        }
    }

    /// <summary>
    /// Gets the avatar text based on message sender
    /// </summary>
    public string AvatarText => Message?.Sender switch
    {
        MessageSender.User => "U",
        MessageSender.Assistant => "AI",
        MessageSender.System => "S",
        _ => "?"
    };

    /// <summary>
    /// Event raised when a command is copied
    /// </summary>
    public event EventHandler<CommandCopiedEventArgs>? CommandCopied;

    /// <summary>
    /// Initializes a new instance of ChatMessageControl
    /// </summary>
    public ChatMessageControl(ILogger<ChatMessageControl> logger)
    {
        InitializeComponent();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (Message == null)
        {
            return;
        }

        var style = Message.Sender switch
        {
            MessageSender.User => GetUserMessageStyle(),
            MessageSender.Assistant => GetAssistantMessageStyle(),
            MessageSender.System => GetSystemMessageStyle(),
            _ => GetDefaultMessageStyle()
        };

        ApplyStyle(style);
    }

    private MessageStyle GetUserMessageStyle()
    {
        return new MessageStyle
        {
            Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
            AvatarBackground = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
            SenderNameColor = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
            TimestampColor = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
            MessageTextColor = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
            MetadataColor = new SolidColorBrush(Color.FromRgb(189, 195, 199))
        };
    }

    private MessageStyle GetAssistantMessageStyle()
    {
        return new MessageStyle
        {
            Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
            AvatarBackground = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
            SenderNameColor = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            TimestampColor = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
            MessageTextColor = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            MetadataColor = new SolidColorBrush(Color.FromRgb(127, 140, 141))
        };
    }

    private MessageStyle GetSystemMessageStyle()
    {
        return new MessageStyle
        {
            Background = new SolidColorBrush(Color.FromRgb(241, 196, 15)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(211, 84, 0)),
            AvatarBackground = new SolidColorBrush(Color.FromRgb(211, 84, 0)),
            SenderNameColor = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            TimestampColor = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            MessageTextColor = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            MetadataColor = new SolidColorBrush(Color.FromRgb(255, 255, 255))
        };
    }

    private MessageStyle GetDefaultMessageStyle()
    {
        return new MessageStyle
        {
            Background = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
            AvatarBackground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
            SenderNameColor = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            TimestampColor = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
            MessageTextColor = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            MetadataColor = new SolidColorBrush(Color.FromRgb(127, 140, 141))
        };
    }

    private void ApplyStyle(MessageStyle style)
    {
        var resources = Resources;
        resources["MessageBackground"] = style.Background;
        resources["MessageBorderBrush"] = style.BorderBrush;
        resources["AvatarBackground"] = style.AvatarBackground;
        resources["SenderNameColor"] = style.SenderNameColor;
        resources["TimestampColor"] = style.TimestampColor;
        resources["MessageTextColor"] = style.MessageTextColor;
        resources["MetadataColor"] = style.MetadataColor;
    }

    private class MessageStyle
    {
        public SolidColorBrush Background { get; set; }
        public SolidColorBrush BorderBrush { get; set; }
        public SolidColorBrush AvatarBackground { get; set; }
        public SolidColorBrush SenderNameColor { get; set; }
        public SolidColorBrush TimestampColor { get; set; }
        public SolidColorBrush MessageTextColor { get; set; }
        public SolidColorBrush MetadataColor { get; set; }
    }
}
