using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Configuration;

/// <summary>
/// Application user settings
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Version of settings file
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Context window settings
    /// </summary>
    public ContextWindowSettings? ContextWindow { get; set; }

    /// <summary>
    /// LLM provider settings
    /// </summary>
    public List<ProviderSettings>? Providers { get; set; }

    /// <summary>
    /// Active provider ID
    /// </summary>
    public string? ActiveProvider { get; set; }

    /// <summary>
    /// Theme settings
    /// </summary>
    public ThemeSettings? Theme { get; set; }

    /// <summary>
    /// Application settings
    /// </summary>
    public ApplicationSettings? Application { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public UserSettings()
    {
        ContextWindow = new ContextWindowSettings();
        Providers = new List<ProviderSettings>();
        Theme = new ThemeSettings();
        Application = new ApplicationSettings();
    }
}

/// <summary>
/// Context window settings
/// </summary>
public class ContextWindowSettings
{
    /// <summary>
    /// Size mode (Auto/Fixed/Percentage)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Context.SizeMode SizeMode { get; set; } = Context.ContextSizeMode.Auto;

    /// <summary>
    /// Fixed size (in tokens)
    /// </summary>
    public int FixedSize { get; set; } = 2000;

    /// <summary>
    /// Percentage of model's max context (0.0 to 1.0)
    /// </summary>
    public double Percentage { get; set; } = 0.5;

    /// <summary>
    /// Minimum number of lines
    /// </summary>
    public int MinLines { get; set; } = 10;

    /// <summary>
    /// Maximum number of lines
    /// </summary>
    public int MaxLines { get; set; } = 500;

    /// <summary>
    /// Minimum number of tokens
    /// </summary>
    public int MinTokens { get; set; } = 100;

    /// <summary>
    /// Maximum number of tokens
    /// </summary>
    public int MaxTokens { get; set; } = 128000;

    /// <summary>
    /// Include system prompt in context
    /// </summary>
    public bool IncludeSystemPrompt { get; set; } = true;

    /// <summary>
    /// Include working directory in context
    /// </summary>
    public bool IncludeWorkingDirectory { get; set; } = true;

    /// <summary>
    /// Include privilege level in context
    /// </summary>
    public bool IncludePrivilegeLevel { get; set; } = true;

    /// <summary>
    /// Default system prompt
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant for terminal administration.";

    /// <summary>
    /// Creates a ContextWindowSettings from this
    /// </summary>
    public Context.ContextWindowSettings ToContextSettings()
    {
        return new Context.ContextWindowSettings
        {
            SizeMode = SizeMode,
            FixedSize = FixedSize,
            Percentage = Percentage,
            MinLines = MinLines,
            MaxLines = MaxLines,
            MinTokens = MinTokens,
            MaxTokens = MaxTokens
        };
    }
}

/// <summary>
/// LLM provider settings
/// </summary>
public class ProviderSettings
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public string ProviderId { get; set; }

    /// <summary>
    /// API key (encrypted storage recommended)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model to use
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Temperature
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Organization ID (OpenAI)
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Theme settings
/// </summary>
public class ThemeSettings
{
    /// <summary>
    /// Theme mode (Light/Dark/System)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ThemeMode Mode { get; set; } = ThemeMode.System;

    /// <summary>
    /// Accent color
    /// </summary>
    public string AccentColor { get; set; } = "#3498DB";

    /// <summary>
    /// Font size
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Font family
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";
}

/// <summary>
/// Theme mode
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System
}

/// <summary>
/// Application settings
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Window width
    /// </summary>
    public int WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Window height
    /// </summary>
    public int WindowHeight { get; set; } = 800;

    /// <summary>
    /// Terminal pane width percentage
    /// </summary>
    public double TerminalWidthPercentage { get; set; } = 0.5;

    /// <summary>
    /// Auto-save interval (minutes)
    /// </summary>
    public int AutoSaveInterval { get; set; } = 5;

    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Check for updates on startup
    /// </summary>
    public bool CheckForUpdates { get; set; } = true;
}

/// <summary>
/// JSON converter for enum to string
/// </summary>
public class JsonStringEnumConverter : JsonConverter<Enum>
{
    public override Enum Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
        {
            throw new JsonException("Cannot convert null to Enum");
        }

        return (Enum)Enum.Parse(typeToConvert, value, ignoreCase: true);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Enum value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Settings provider for persistence
/// </summary>
public class SettingsProvider
{
    private readonly ILogger<SettingsProvider> _logger;
    private readonly string _settingsPath;
    private UserSettings? _cachedSettings;

    private const string SettingsFileName = "settings.json";

    /// <summary>
    /// Default settings file path
    /// </summary>
    private static readonly string DefaultSettingsPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PairAdmin",
            SettingsFileName);

    /// <summary>
    /// Initializes a new instance of SettingsProvider
    /// </summary>
    public SettingsProvider(
        ILogger<SettingsProvider> logger,
        string? settingsPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsPath = settingsPath ?? DefaultSettingsPath;

        _logger.LogInformation($"SettingsProvider initialized with path: {_settingsPath}");
    }

    /// <summary>
    /// Loads settings from file
    /// </summary>
    /// <returns>Loaded settings or default if not found</returns>
    public async Task<UserSettings> LoadSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation($"Settings file not found: {_settingsPath}, using defaults");
                return new UserSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);

            if (settings != null)
            {
                _cachedSettings = settings;
                _logger.LogInformation("Settings loaded successfully");
                return settings;
            }

            _logger.LogWarning("Failed to deserialize settings, using defaults");
            return new UserSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Saves settings to file
    /// </summary>
    /// <param name="settings">Settings to save</param>
    public async Task SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_settingsPath, json);

            _cachedSettings = settings;
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw new InvalidOperationException("Failed to save settings", ex);
        }
    }

    /// <summary>
    /// Gets cached settings
    /// </summary>
    /// <returns>Cached settings or null</returns>
    public UserSettings? GetCachedSettings()
    {
        return _cachedSettings;
    }

    /// <summary>
    /// Resets settings to defaults
    /// </summary>
    public UserSettings ResetToDefaults()
    {
        return new UserSettings();
    }

    /// <summary>
    /// Gets the settings file path
    /// </summary>
    public string GetSettingsPath()
    {
        return _settingsPath;
    }

    /// <summary>
    /// Checks if settings file exists
    /// </summary>
    /// <returns>True if settings file exists</returns>
    public bool SettingsFileExists()
    {
        return File.Exists(_settingsPath);
    }

    /// <summary>
    /// Deletes the settings file
    /// </summary>
    public void DeleteSettings()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
            _logger.LogInformation("Settings file deleted");
        }
    }
}
