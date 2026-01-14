using System;
using System.IO;
using System.Text.Json;

namespace PairAdmin.UI.Terminal;

/// <summary>
/// Terminal mode selection
/// </summary>
public enum TerminalMode
{
    /// <summary>Not yet configured - will prompt user on first launch</summary>
    NotConfigured = 0,

    /// <summary>Use integrated PairAdminPuTTY.dll (bundled, may be older version)</summary>
    Integrated = 1,

    /// <summary>Use external putty.exe (user maintains separately)</summary>
    External = 2
}

/// <summary>
/// Terminal configuration settings
/// </summary>
public class TerminalSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PairAdmin",
        "terminal-settings.json");

    /// <summary>
    /// Selected terminal mode
    /// </summary>
    public TerminalMode Mode { get; set; } = TerminalMode.NotConfigured;

    /// <summary>
    /// Custom path to external PuTTY executable (optional)
    /// </summary>
    public string? ExternalPuTTYPath { get; set; }

    /// <summary>
    /// Last connected hostname
    /// </summary>
    public string? LastHostname { get; set; }

    /// <summary>
    /// Last connected port
    /// </summary>
    public int LastPort { get; set; } = 22;

    /// <summary>
    /// Last connected username
    /// </summary>
    public string? LastUsername { get; set; }

    /// <summary>
    /// Load settings from disk
    /// </summary>
    public static TerminalSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<TerminalSettings>(json) ?? new TerminalSettings();
            }
        }
        catch
        {
            // Return default settings on error
        }
        return new TerminalSettings();
    }

    /// <summary>
    /// Save settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail - settings are not critical
        }
    }

    /// <summary>
    /// Check if integrated PuTTY DLL is available
    /// </summary>
    public static bool IsIntegratedAvailable()
    {
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PairAdminPuTTY.dll");
        return File.Exists(dllPath);
    }

    /// <summary>
    /// Check if external PuTTY is available
    /// </summary>
    public static bool IsExternalAvailable(string? customPath = null)
    {
        // Check custom path first
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return true;

        // Check common installation paths
        var paths = new[]
        {
            "putty.exe", // In PATH
            @"C:\Program Files\PuTTY\putty.exe",
            @"C:\Program Files (x86)\PuTTY\putty.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\PuTTY\putty.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"PuTTY\putty.exe")
        };

        foreach (var path in paths)
        {
            if (path == "putty.exe")
            {
                // Check if in PATH
                try
                {
                    var result = Environment.GetEnvironmentVariable("PATH");
                    if (result != null)
                    {
                        foreach (var dir in result.Split(';'))
                        {
                            var fullPath = Path.Combine(dir, "putty.exe");
                            if (File.Exists(fullPath))
                                return true;
                        }
                    }
                }
                catch { }
            }
            else if (File.Exists(path))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Find external PuTTY path
    /// </summary>
    public static string? FindExternalPuTTY(string? customPath = null)
    {
        // Check custom path first
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        // Check common installation paths
        var paths = new[]
        {
            @"C:\Program Files\PuTTY\putty.exe",
            @"C:\Program Files (x86)\PuTTY\putty.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\PuTTY\putty.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"PuTTY\putty.exe")
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        // Check PATH
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(';'))
                {
                    var fullPath = Path.Combine(dir.Trim(), "putty.exe");
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
        }
        catch { }

        return null;
    }
}
