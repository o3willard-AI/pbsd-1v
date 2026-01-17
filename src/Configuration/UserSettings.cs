namespace Configuration;

/// <summary>
/// Theme mode for UI
/// </summary>
public enum ThemeMode
{
    Light,
    Dark,
    System
}

/// <summary>
/// Settings provider interface
/// </summary>
public class SettingsProvider
{
    private static UserSettings _settings = new();

    /// <summary>
    /// Gets the current settings
    /// </summary>
    public static UserSettings Current => _settings;

    /// <summary>
    /// Gets a setting value
    /// </summary>
    public T? Get<T>(string key)
    {
        return default;
    }

    /// <summary>
    /// Sets a setting value
    /// </summary>
    public void Set<T>(string key, T value)
    {
    }

    /// <summary>
    /// Saves settings to disk
    /// </summary>
    public void Save()
    {
    }

    /// <summary>
    /// Loads settings from disk
    /// </summary>
    public void Load()
    {
    }

    /// <summary>
    /// Saves settings asynchronously
    /// </summary>
    public Task SaveSettingsAsync(UserSettings settings)
    {
        _settings = settings;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads settings asynchronously
    /// </summary>
    public Task<UserSettings> LoadSettingsAsync()
    {
        return Task.FromResult(_settings);
    }

    /// <summary>
    /// Gets the settings file path
    /// </summary>
    public string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PairAdmin",
            "settings.json");
    }

    /// <summary>
    /// Checks if settings file exists
    /// </summary>
    public bool SettingsFileExists()
    {
        return File.Exists(GetSettingsPath());
    }
}

/// <summary>
/// User settings for PairAdmin
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Selected model ID
    /// </summary>
    public string ModelId { get; set; } = "gpt-4";

    /// <summary>
    /// Current theme settings
    /// </summary>
    public ThemeSettings? Theme { get; set; }

    /// <summary>
    /// API key for the selected provider
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Maximum context window size in tokens
    /// </summary>
    public int MaxContextTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for model responses
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Enable auto-save of conversations
    /// </summary>
    public bool AutoSaveConversations { get; set; } = true;

    /// <summary>
    /// Font size for terminal
    /// </summary>
    public int TerminalFontSize { get; set; } = 12;

    /// <summary>
    /// Font family for terminal
    /// </summary>
    public string TerminalFontFamily { get; set; } = "Consolas";
}

/// <summary>
/// Theme settings for the application
/// </summary>
public class ThemeSettings
{
    /// <summary>
    /// Theme mode (Light, Dark, System)
    /// </summary>
    public ThemeMode Mode { get; set; } = ThemeMode.System;

    /// <summary>
    /// Accent color in hex format (#RRGGBB)
    /// </summary>
    public string AccentColor { get; set; } = "#3498DB";

    /// <summary>
    /// Font size in pixels
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Font family name
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";
}
