using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PairAdmin.Chat;

namespace PairAdmin.UI.Controls;

/// <summary>
/// Converter that returns true if text is not empty
/// </summary>
public class NotEmptyConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Main chat pane control for AI assistant
/// </summary>
public partial class ChatPane : UserControl
{
    private readonly ObservableCollection<ChatMessage> _messages;
    private readonly ILogger<ChatPane> _logger;
    private bool _isProcessing;

    /// <summary>
    /// Gets the collection of chat messages
    /// </summary>
    public ObservableCollection<ChatMessage> Messages => _messages;

    /// <summary>
    /// Event raised when a message is sent
    /// </summary>
    public event EventHandler<MessageSentEventArgs>? MessageSent;

    /// <summary>
    /// Event raised when a command is copied
    /// </summary>
    public event EventHandler<CommandCopiedEventArgs>? CommandCopied;

    /// <summary>
    /// Event raised when history is cleared
    /// </summary>
    public event EventHandler? HistoryCleared;

    /// <summary>
    /// Gets or sets whether the chat is currently processing
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            UpdateUIState();
        }
    }

    /// <summary>
    /// Initializes a new instance of ChatPane (for XAML)
    /// </summary>
    public ChatPane() : this(NullLogger<ChatPane>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of ChatPane
    /// </summary>
    public ChatPane(ILogger<ChatPane> logger)
    {
        InitializeComponent();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messages = new ObservableCollection<ChatMessage>();

        MessagesItemsControl.ItemsSource = _messages;

        UpdateTokenCount(0);

        _logger.LogInformation("ChatPane initialized");
    }

    /// <summary>
    /// Adds a user message to the chat
    /// </summary>
    /// <param name="content">Message content</param>
    public void AddUserMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Cannot add empty message");
            return;
        }

        var message = new ChatMessage
        {
            Sender = MessageSender.User,
            Content = content.Trim(),
            Timestamp = DateTime.Now
        };

        AddMessage(message);

        MessageSent?.Invoke(this, new MessageSentEventArgs { Message = message });

        _logger.LogInformation($"User message added: {message.Id}");
    }

    /// <summary>
    /// Adds an AI assistant message to the chat
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="isStreaming">Whether message is streaming</param>
    public void AddAssistantMessage(string content, bool isStreaming = false)
    {
        var message = new ChatMessage
        {
            Sender = MessageSender.Assistant,
            Content = content,
            Timestamp = DateTime.Now,
            IsStreaming = isStreaming
        };

        if (!isStreaming)
        {
            message.ExtractCommandBlocks();
        }

        AddMessage(message);

        _logger.LogInformation($"Assistant message added: {message.Id}");
    }

    /// <summary>
    /// Updates the last assistant message (for streaming)
    /// </summary>
    /// <param name="additionalContent">Additional content to append</param>
    public void UpdateLastAssistantMessage(string additionalContent)
    {
        if (_messages.Count == 0)
        {
            _logger.LogWarning("No messages to update");
            return;
        }

        var lastMessage = _messages.Last();
        if (lastMessage.Sender == MessageSender.Assistant)
        {
            lastMessage.AppendContent(additionalContent);
            _logger.LogTrace($"Updated message {lastMessage.Id} with additional content");
        }
        else
        {
            _logger.LogWarning("Last message is not from assistant");
        }
    }

    /// <summary>
    /// Marks the last assistant message as complete
    /// </summary>
    public void CompleteLastAssistantMessage()
    {
        if (_messages.Count == 0)
        {
            _logger.LogWarning("No messages to complete");
            return;
        }

        var lastMessage = _messages.Last();
        if (lastMessage.Sender == MessageSender.Assistant)
        {
            lastMessage.Complete();
            _logger.LogInformation($"Message {lastMessage.Id} completed");
        }
        else
        {
            _logger.LogWarning("Last message is not from assistant");
        }
    }

    /// <summary>
    /// Clears all messages from the chat
    /// </summary>
    public void ClearHistory()
    {
        var count = _messages.Count;
        _messages.Clear();
        HistoryCleared?.Invoke(this, EventArgs.Empty);
        UpdateTokenCount(0);
        _logger.LogInformation($"Chat history cleared. Removed {count} messages");
    }

    /// <summary>
    /// Copies the last command block to clipboard
    /// </summary>
    public void CopyLastCommand()
    {
        var lastCommandBlock = FindLastCommandBlock();
        if (lastCommandBlock != null)
        {
            CopyCommandToClipboard(lastCommandBlock);
        }
        else
        {
            _logger.LogWarning("No command blocks found in chat history");
            ShowMessage("No commands found in chat history");
        }
    }

    private void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        ScrollToBottom();
        UpdateTokenCount(message.TokenCount);
    }

    private CommandBlock? FindLastCommandBlock()
    {
        foreach (var message in _messages.Reverse())
        {
            if (message.HasCommandBlocks)
            {
                return message.CommandBlocks.Last();
            }
        }

        return null;
    }

    private void CopyCommandToClipboard(CommandBlock commandBlock)
    {
        try
        {
            Clipboard.SetText(commandBlock.Code);
            CommandCopied?.Invoke(this, new CommandCopiedEventArgs
            {
                CommandBlock = commandBlock,
                CopiedText = commandBlock.Code
            });

            ShowMessage("Command copied to clipboard");
            _logger.LogInformation($"Command copied: {commandBlock.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy command to clipboard");
            ShowMessage("Failed to copy command");
        }
    }

    private void ScrollToBottom()
    {
        Dispatcher.Invoke(() =>
        {
            MessageScrollViewer.ScrollToBottom();
        });
    }

    private void UpdateTokenCount(int tokenCount)
    {
        var totalTokens = _messages.Sum(m => m.TokenCount) + tokenCount;
        TokenCountText.Text = $"Tokens: {totalTokens}";
    }

    private void UpdateUIState()
    {
        SendButton.Content = IsProcessing ? "⏳ Sending..." : "Send";
        SendButton.IsEnabled = !IsProcessing && !string.IsNullOrWhiteSpace(InputTextBox.Text);
        InputTextBox.IsEnabled = !IsProcessing;
    }

    private void ShowMessage(string message)
    {
        MessageBox.Show(message, "PairAdmin", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            _logger.LogWarning("Cannot send empty message");
            return;
        }

        AddUserMessage(InputTextBox.Text);
        InputTextBox.Clear();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all chat history?",
            "Clear History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ClearHistory();
        }
    }

    private void CopyLastButton_Click(object sender, RoutedEventArgs e)
    {
        CopyLastCommand();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Settings button clicked");
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            e.Handled = true;
            var textBox = sender as TextBox;
            var caretIndex = textBox.CaretIndex;
            textBox.Text = textBox.Text.Insert(caretIndex, Environment.NewLine);
            textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SendButton_Click(sender, e);
        }
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var tokenCount = EstimateTokenCount(textBox.Text);
        TokenCountText.Text = $"Tokens: {tokenCount}";
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Length / 4;
    }
}
