using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using PairAdmin.Chat;

namespace PairAdmin.UI.Controls;

/// <summary>
/// Control for displaying code/command blocks
/// </summary>
public partial class CommandBlockControl : UserControl
{
    private CommandBlock? _commandBlock;
    private readonly ILogger<CommandBlockControl> _logger;

    /// <summary>
    /// Gets or sets the command block to display
    /// </summary>
    public CommandBlock? CommandBlock
    {
        get => _commandBlock;
        set
        {
            _commandBlock = value;
            DataContext = value;
        }
    }

    /// <summary>
    /// Gets or sets the code content
    /// </summary>
    public string Code
    {
        get => _commandBlock?.Code ?? string.Empty;
        set
        {
            if (_commandBlock != null)
            {
                _commandBlock.Code = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the language
    /// </summary>
    public string Language
    {
        get => _commandBlock?.Language ?? string.Empty;
        set
        {
            if (_commandBlock != null)
            {
                _commandBlock.Language = value;
            }
        }
    }

    /// <summary>
    /// Event raised when command is copied
    /// </summary>
    public event EventHandler<CommandCopiedEventArgs>? CommandCopied;

    /// <summary>
    /// Initializes a new instance of CommandBlockControl
    /// </summary>
    public CommandBlockControl(ILogger<CommandBlockControl> logger)
    {
        InitializeComponent();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance with a command block
    /// </summary>
    public CommandBlockControl(CommandBlock commandBlock, ILogger<CommandBlockControl> logger) : this(logger)
    {
        CommandBlock = commandBlock;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_commandBlock == null)
        {
            _logger.LogWarning("No command block to copy");
            return;
        }

        try
        {
            Clipboard.SetText(_commandBlock.Code);

            CommandCopied?.Invoke(this, new CommandCopiedEventArgs
            {
                CommandBlock = _commandBlock,
                CopiedText = _commandBlock.Code
            });

            ShowCopyFeedback(sender);
            _logger.LogInformation($"Command copied: {_commandBlock.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy command to clipboard");
            ShowErrorFeedback();
        }
    }

    private void ShowCopyFeedback(object sender)
    {
        var originalContent = (sender as Button)?.Content;

        if (sender is Button button)
        {
            button.Content = "✓";
            button.IsEnabled = false;

            Dispatcher.Invoke(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                button.Content = originalContent;
                button.IsEnabled = true;
            });
        }
    }

    private void ShowErrorFeedback()
    {
        MessageBox.Show(
            "Failed to copy command to clipboard",
            "Copy Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ApplySyntaxHighlighting()
    {
        if (_commandBlock == null)
        {
            return;
        }

        var highlightedCode = HighlightCode(_commandBlock.Code, _commandBlock.Language);
        CodeTextBox.Text = highlightedCode;
    }

    private string HighlightCode(string code, string language)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        return language.ToLower() switch
        {
            "bash" or "sh" or "shell" => HighlightBashCode(code),
            "python" or "py" => HighlightPythonCode(code),
            "powershell" or "ps1" => HighlightPowerShellCode(code),
            "csharp" or "c#" => HighlightCSharpCode(code),
            _ => code
        };
    }

    private string HighlightBashCode(string code)
    {
        return code;

    }

    private string HighlightPythonCode(string code)
    {
        return code;

    }

    private string HighlightPowerShellCode(string code)
    {
        return code;

    }

    private string HighlightCSharpCode(string code)
    {
        return code;

    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySyntaxHighlighting();
    }
}
