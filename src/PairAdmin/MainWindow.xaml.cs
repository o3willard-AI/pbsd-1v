using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PairAdmin.UI;
using PairAdmin.UI.Controls;
using PairAdmin.Chat;
using PairAdmin.LLMGateway;
using PairAdmin.LLMGateway.Models;
using PairAdmin.LLMGateway.Providers;

namespace PairAdmin;

/// <summary>
/// Main window for PairAdmin application
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly WindowStateManager _windowStateManager;
    private readonly PairAdmin.LLMGateway.LLMGateway _llmGateway;
    private ChatPane? _chatPane;

    /// <summary>
    /// Initializes a new instance of MainWindow class
    /// </summary>
    public MainWindow(
        ILogger<MainWindow> logger, 
        ILoggerFactory loggerFactory,
        WindowStateManager windowStateManager,
        PairAdmin.LLMGateway.LLMGateway llmGateway)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _windowStateManager = windowStateManager ?? throw new ArgumentNullException(nameof(windowStateManager));
        _llmGateway = llmGateway ?? throw new ArgumentNullException(nameof(llmGateway));
        InitializeComponent();

        // Subscribe to window events
        this.StateChanged += MainWindow_StateChanged;
        this.Closing += MainWindow_Closing;

        // Restore window state if available
        RestoreWindowState();

        // Configure LLM Gateway
        ConfigureLLMGateway();

        // Subscribe to chat events
        _chatPane = (ChatPane)FindName("ChatPaneControl");
        if (_chatPane != null)
        {
            _chatPane.MessageSent += OnChatMessageSent;
        }
    }

    private void ConfigureLLMGateway()
    {
        try
        {
            _llmGateway.RegisterProvider(new OpenAIProvider(
                _loggerFactory.CreateLogger<OpenAIProvider>()));
            _llmGateway.RegisterProvider(new GeminiProvider(
                _loggerFactory.CreateLogger<GeminiProvider>()));
            _llmGateway.RegisterProvider(new OpenRouterProvider(
                _loggerFactory.CreateLogger<OpenRouterProvider>()));
            _llmGateway.RegisterProvider(new OllamaProvider(
                _loggerFactory.CreateLogger<OllamaProvider>()));
            
            _llmGateway.SetActiveProvider("openai");
            _logger.LogInformation("LLM Gateway configured with providers: OpenAI, Gemini, OpenRouter, Ollama");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure LLM Gateway");
        }
    }

    private async void OnChatMessageSent(object? sender, MessageSentEventArgs e)
    {
        if (e.Message == null) return;

        _logger.LogInformation("Processing chat message: {MessageId}", e.Message.Id);
        _logger.LogInformation("Active provider: {Provider}", _llmGateway.ActiveProvider?.GetProviderInfo().Id ?? "null");
        _logger.LogInformation("Active provider configured: {Configured}", _llmGateway.ActiveProvider?.IsConfigured ?? false);

        try
        {
            if (_chatPane != null)
            {
                _chatPane.IsProcessing = true;
            }

            // Check if provider is configured
            if (_llmGateway.ActiveProvider == null)
            {
                _chatPane?.AddAssistantMessage("No LLM provider configured. Please configure an API key in Settings.");
                _logger.LogWarning("No active LLM provider - prompting user to configure");
                return;
            }

            // Check if provider is configured with API key
            var providerId = _llmGateway.ActiveProvider.GetProviderInfo().Id;
            if (providerId == "openai" || providerId == "gemini" || providerId == "openrouter")
            {
                if (!_llmGateway.ActiveProvider.IsConfigured)
                {
                    _chatPane?.AddAssistantMessage($"Please configure your API key for {_llmGateway.ActiveProvider.GetProviderInfo().Name} in Settings.");
                    _logger.LogWarning("Provider {Provider} not configured with API key", providerId);
                    return;
                }
            }

            // Create context for the AI
            var request = new CompletionRequest
            {
                Messages = new List<Message>
                {
                    Message.UserMessage(e.Message.Content)
                },
                MaxTokens = 1024,
                Temperature = 0.7
            };

            _logger.LogInformation("Sending request to LLM provider: {Provider}", providerId);

            // Send to LLM
            var response = await _llmGateway.SendAsync(request);
            
            if (response?.Content != null)
            {
                _chatPane?.AddAssistantMessage(response.Content);
                _logger.LogInformation("AI response received");
            }
            else
            {
                _logger.LogWarning("LLM response was null or empty");
                _chatPane?.AddAssistantMessage("No response from LLM provider.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI response");
            _chatPane?.AddAssistantMessage($"Error: {ex.Message}");
        }
        finally
        {
            if (_chatPane != null)
            {
                _chatPane.IsProcessing = false;
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("Settings button clicked");
        
        var settingsDialog = new SettingsDialog(
            App.Services!.GetRequiredService<IConfiguration>(),
            _llmGateway);
        
        settingsDialog.Owner = this;
        settingsDialog.ShowDialog();
    }

    /// <summary>
    /// Window state changed event handler
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        _logger.LogDebug("Window state changed: {WindowState}", this.WindowState);

        // Save window state when changed (but not while maximized)
        if (this.WindowState == System.Windows.WindowState.Normal)
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
