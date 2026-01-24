using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using PairAdmin.LLMGateway;
using PairAdmin.LLMGateway.Providers;

namespace PairAdmin;

public partial class SettingsDialog : Window
{
    private readonly IConfiguration _configuration;
    private readonly PairAdmin.LLMGateway.LLMGateway _llmGateway;
    private string _selectedProvider = "openai";

    private readonly Dictionary<string, List<string>> _models = new()
    {
        ["openai"] = new() { "gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o" },
        ["gemini"] = new() { "gemini-pro", "gemini-1.5-pro", "gemini-1.5-flash" },
        ["openrouter"] = new() { "openrouter/auto", "anthropic/claude-3-opus", "openai/gpt-4" },
        ["ollama"] = new() { "llama3", "llama3.1", "mistral", "codellama", "gemma", "phi3" }
    };

    public SettingsDialog(IConfiguration configuration, PairAdmin.LLMGateway.LLMGateway llmGateway)
    {
        InitializeComponent();
        _configuration = configuration;
        _llmGateway = llmGateway;
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        var llmSection = _configuration.GetSection("LLM");
        var defaultProvider = llmSection.GetValue<string>("DefaultProvider") ?? "openai";
        
        // Set provider combo
        foreach (ComboBoxItem item in ProviderComboBox.Items)
        {
            if (item.Tag?.ToString() == defaultProvider)
            {
                ProviderComboBox.SelectedItem = item;
                break;
            }
        }
        
        // Load API keys
        var openaiKey = llmSection.GetSection("OpenAI").GetValue<string>("ApiKey") ?? "";
        var geminiKey = llmSection.GetSection("Google").GetValue<string>("ApiKey") ?? "";
        var openrouterKey = llmSection.GetSection("OpenRouter").GetValue<string>("ApiKey") ?? "";
        var ollamaUrl = llmSection.GetSection("Ollama").GetValue<string>("BaseUrl") ?? "http://localhost:11434";
        
        // Store in Tag for saving later
        ApiKeyPasswordBox.Tag = new Dictionary<string, string>
        {
            ["openai"] = openaiKey,
            ["gemini"] = geminiKey,
            ["openrouter"] = openrouterKey
        };
        OllamaUrlTextBox.Text = ollamaUrl;
        
        UpdateModelComboBox();
    }

    private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderComboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedProvider = item.Tag?.ToString() ?? "openai";
            
            ApiKeyPanel.Visibility = _selectedProvider == "ollama" 
                ? Visibility.Collapsed 
                : Visibility.Visible;
                
            OllamaUrlPanel.Visibility = _selectedProvider == "ollama" 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            
            // Update model combo
            UpdateModelComboBox();
            
            // Show current API key
            if (ApiKeyPasswordBox.Tag is Dictionary<string, string> keys)
            {
                if (keys.TryGetValue(_selectedProvider, out var key))
                {
                    ApiKeyPasswordBox.Password = key;
                }
            }
        }
    }

    private void UpdateModelComboBox()
    {
        ModelComboBox.Items.Clear();
        if (_models.TryGetValue(_selectedProvider, out var models))
        {
            foreach (var model in models)
            {
                ModelComboBox.Items.Add(new ComboBoxItem { Content = model, Tag = model });
            }
            
            // Select default model
            var llmSection = _configuration.GetSection("LLM");
            var sectionName = _selectedProvider switch
            {
                "openai" => "OpenAI",
                "gemini" => "Google",
                "openrouter" => "OpenRouter",
                "ollama" => "Ollama",
                _ => "OpenAI"
            };
            var savedModel = llmSection.GetSection(sectionName).GetValue<string>("Model") ?? "";
            
            foreach (ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Tag?.ToString() == savedModel || 
                    (string.IsNullOrEmpty(savedModel) && item.Tag?.ToString() == models.First()))
                {
                    ModelComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        ConnectionStatusText.Text = "Testing...";
        ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Colors.Yellow);
        
        bool success = false;
        string message = "";
        
        try
        {
            if (_selectedProvider == "ollama")
            {
                using var http = new HttpClient();
                http.Timeout = System.TimeSpan.FromSeconds(5);
                var response = await http.GetAsync(OllamaUrlTextBox.Text.Trim() + "/api/tags");
                success = response.IsSuccessStatusCode;
                message = success ? "Connected to Ollama!" : "Ollama not responding";
            }
            else
            {
                var apiKey = ApiKeyPasswordBox.Password;
                if (string.IsNullOrEmpty(apiKey))
                {
                    message = "Please enter an API key";
                    success = false;
                }
                else
                {
                    // Actually test the API
                    using var http = new HttpClient();
                    http.Timeout = System.TimeSpan.FromSeconds(10);
                    
                    string testUrl = "";
                    Dictionary<string, string>? headers = null;
                    
                    switch (_selectedProvider)
                    {
                        case "openrouter":
                            testUrl = "https://openrouter.ai/api/v1/models";
                            headers = new Dictionary<string, string>
                            {
                                ["Authorization"] = $"Bearer {apiKey}",
                                ["HTTP-Referer"] = "https://pairadmin.app",
                                ["X-Title"] = "PairAdmin"
                            };
                            break;
                        case "gemini":
                            testUrl = $"https://generativelanguage.googleapis.com/v1/models?key={apiKey}";
                            break;
                        case "openai":
                            testUrl = "https://api.openai.com/v1/models";
                            headers = new Dictionary<string, string>
                            {
                                ["Authorization"] = $"Bearer {apiKey}"
                            };
                            break;
                    }
                    
                    if (headers != null)
                    {
                        foreach (var h in headers)
                            http.DefaultRequestHeaders.Add(h.Key, h.Value);
                    }
                    
                    var response = await http.GetAsync(testUrl);
                    success = response.IsSuccessStatusCode;
                    
                    if (success)
                    {
                        message = $"Connected to {_selectedProvider.ToUpper()} successfully!";
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        message = $"Error: {response.StatusCode} - {errorContent.Substring(0, Math.Min(100, errorContent.Length))}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            message = $"Connection failed: {ex.Message}";
            success = false;
        }
        
        ConnectionStatusText.Text = message;
        ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(
            success ? System.Windows.Media.Colors.LightGreen : System.Windows.Media.Colors.LightCoral);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, 
                "appsettings.json");
            
            if (!File.Exists(configPath))
            {
                configPath = "appsettings.json";
            }
            
            var json = File.ReadAllText(configPath);
            var config = System.Text.Json.JsonDocument.Parse(json);
            var root = config.RootElement;
            
            // Get API keys from Tag
            var keys = ApiKeyPasswordBox.Tag as Dictionary<string, string> ?? new Dictionary<string, string>();
            keys[_selectedProvider] = ApiKeyPasswordBox.Password;
            ApiKeyPasswordBox.Tag = keys;
            
            // Build new config
            var newConfig = new Dictionary<string, object>();
            
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "LLM")
                {
                    var llm = new Dictionary<string, object>();
                    llm["DefaultProvider"] = _selectedProvider;
                    
                    // OpenAI
                    llm["OpenAI"] = new Dictionary<string, object>
                    {
                        ["ApiKey"] = keys.TryGetValue("openai", out var ok) ? ok : "",
                        ["BaseUrl"] = "https://api.openai.com/v1",
                        ["Model"] = GetSelectedModel() == "gpt-3.5-turbo" ? "gpt-3.5-turbo" : GetSelectedModel(),
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.7
                    };
                    
                    // Google
                    llm["Google"] = new Dictionary<string, object>
                    {
                        ["ApiKey"] = keys.TryGetValue("gemini", out var gk) ? gk : "",
                        ["BaseUrl"] = "https://generativelanguage.googleapis.com/v1",
                        ["Model"] = GetSelectedModel() == "gemini-pro" ? "gemini-pro" : GetSelectedModel(),
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.7
                    };
                    
                    // OpenRouter
                    llm["OpenRouter"] = new Dictionary<string, object>
                    {
                        ["ApiKey"] = keys.TryGetValue("openrouter", out var ork) ? ork : "",
                        ["BaseUrl"] = "https://openrouter.ai/api/v1",
                        ["Model"] = GetSelectedModel() == "openrouter/auto" ? "openrouter/auto" : GetSelectedModel(),
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.7
                    };
                    
                    // Ollama
                    llm["Ollama"] = new Dictionary<string, object>
                    {
                        ["BaseUrl"] = OllamaUrlTextBox.Text,
                        ["Model"] = GetSelectedModel() == "llama3" ? "llama3" : GetSelectedModel(),
                        ["MaxTokens"] = 4096,
                        ["Temperature"] = 0.7
                    };
                    
                    newConfig["LLM"] = llm;
                }
                else if (property.Name == "Logging" || property.Name == "Context" || 
                         property.Name == "Security" || property.Name == "UI" || 
                         property.Name == "WindowState")
                {
                    // Keep other sections as-is
                    newConfig[property.Name] = System.Text.Json.JsonSerializer.Deserialize<object>(
                        property.Value.GetRawText())!;
                }
            }
            
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            var newJson = System.Text.Json.JsonSerializer.Serialize(newConfig, options);
            File.WriteAllText(configPath, newJson);
            
            // Prompt user to restart for changes to take effect
            var result = MessageBox.Show(
                "Settings saved! For LLM provider changes to take effect, you'll need to restart the application.\n\nRestart now?",
                "Settings Saved",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            
            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
                Application.Current.Shutdown();
            }
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetSelectedModel()
    {
        if (ModelComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            return item.Tag.ToString() ?? "";
        }
        return _selectedProvider switch
        {
            "openai" => "gpt-3.5-turbo",
            "gemini" => "gemini-pro",
            "openrouter" => "openrouter/auto",
            "ollama" => "llama3",
            _ => ""
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
