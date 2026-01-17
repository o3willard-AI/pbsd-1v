using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ThemeCommandHandler : CommandHandlerBase
{
    private readonly Configuration.SettingsProvider _settingsProvider;

    public ThemeCommandHandler(
        Configuration.SettingsProvider settingsProvider,
        ILogger<ThemeCommandHandler>? logger = null)
        : base(logger)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "theme",
        Description = "Manages the application theme and appearance",
        Syntax = "/theme [show|light|dark|system|<accent-color>|--font-size <size>]",
        Examples =
        [
            "/theme",
            "/theme dark",
            "/theme light",
            "/theme system",
            "/theme #3498DB",
            "/theme --font-size 16"
        ],
        Category = "Configuration",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["t", "color", "style"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (command.Arguments.Count == 0 && command.Flags.Count == 0)
        {
            return ShowCurrentTheme(context);
        }

        if (command.Flags.TryGetValue("font-size", out var fontSizeStr))
        {
            return SetFontSize(context, fontSizeStr);
        }

        if (command.Arguments.Count > 0)
        {
            var arg = command.Arguments[0].ToLowerInvariant();

            return arg switch
            {
                "show" => ShowCurrentTheme(context),
                "light" => SetThemeMode(context, Configuration.ThemeMode.Light),
                "dark" => SetThemeMode(context, Configuration.ThemeMode.Dark),
                "system" => SetThemeMode(context, Configuration.ThemeMode.System),
                _ when arg.StartsWith("#") => SetAccentColor(context, arg),
                _ => InvalidArguments("/theme [show|light|dark|system|#RRGGBB|--font-size <size>]")
            };
        }

        return InvalidArguments("/theme [show|light|dark|system|<accent-color>|--font-size <size>]");
    }

    private CommandResult ShowCurrentTheme(CommandContext context)
    {
        var settings = context.Settings;
        var theme = settings.Theme ?? new Configuration.ThemeSettings();

        var sb = new StringBuilder();

        sb.AppendLine("## Current Theme Settings");
        sb.AppendLine();
        sb.AppendLine($"**Mode:** {theme.Mode}");
        sb.AppendLine($"**Accent Color:** {theme.AccentColor}");
        sb.AppendLine($"**Font Size:** {theme.FontSize}px");
        sb.AppendLine($"**Font Family:** {theme.FontFamily}");
        sb.AppendLine();

        sb.AppendLine("**Available Modes:**");
        sb.AppendLine("- `light` - Light theme");
        sb.AppendLine("- `dark` - Dark theme");
        sb.AppendLine("- `system` - Match system setting");
        sb.AppendLine();

        sb.AppendLine("**Usage:**");
        sb.AppendLine("- `/theme dark` - Switch to dark theme");
        sb.AppendLine("- `/theme #FF5733` - Set accent color");
        sb.AppendLine("- `/theme --font-size 16` - Set font size");

        return Success(sb.ToString());
    }

    private CommandResult SetThemeMode(CommandContext context, Configuration.ThemeMode mode)
    {
        try
        {
            context.Settings.Theme ??= new Configuration.ThemeSettings();
            context.Settings.Theme.Mode = mode;

            _settingsProvider.SaveSettingsAsync(context.Settings).Wait();
            _logger.LogInformation("Set theme mode to {Mode}", mode);

            var sb = new StringBuilder();
            sb.AppendLine("## Theme Updated");
            sb.AppendLine();
            sb.AppendLine($"Theme mode set to **{mode}**.");

            if (mode == Configuration.ThemeMode.System)
            {
                sb.AppendLine("The application will now match your system theme setting.");
            }

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set theme mode");
            return Error(ex.Message);
        }
    }

    private CommandResult SetAccentColor(CommandContext context, string color)
    {
        if (!IsValidColor(color))
        {
            return Error($"Invalid color format: {color}. Use #RRGGBB format (e.g., #3498DB)");
        }

        try
        {
            context.Settings.Theme ??= new Configuration.ThemeSettings();
            context.Settings.Theme.AccentColor = color;

            _settingsProvider.SaveSettingsAsync(context.Settings).Wait();
            _logger.LogInformation("Set accent color to {Color}", color);

            var sb = new StringBuilder();
            sb.AppendLine("## Theme Updated");
            sb.AppendLine();
            sb.AppendLine($"Accent color set to **{color}**.");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set accent color");
            return Error(ex.Message);
        }
    }

    private CommandResult SetFontSize(CommandContext context, string fontSizeStr)
    {
        if (!int.TryParse(fontSizeStr, out var fontSize) || fontSize < 8 || fontSize > 72)
        {
            return Error("Font size must be between 8 and 72 pixels.");
        }

        try
        {
            context.Settings.Theme ??= new Configuration.ThemeSettings();
            context.Settings.Theme.FontSize = fontSize;

            _settingsProvider.SaveSettingsAsync(context.Settings).Wait();
            _logger.LogInformation("Set font size to {FontSize}", fontSize);

            var sb = new StringBuilder();
            sb.AppendLine("## Theme Updated");
            sb.AppendLine();
            sb.AppendLine($"Font size set to **{fontSize}px**.");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set font size");
            return Error(ex.Message);
        }
    }

    private static bool IsValidColor(string color)
    {
        if (!color.StartsWith("#"))
        {
            return false;
        }

        if (color.Length != 7 && color.Length != 9)
        {
            return false;
        }

        return int.TryParse(color.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out _);
    }
}
