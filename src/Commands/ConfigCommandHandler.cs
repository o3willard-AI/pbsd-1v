using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ConfigCommandHandler : CommandHandlerBase
{
    private readonly Configuration.SettingsProvider _settingsProvider;

    public ConfigCommandHandler(
        Configuration.SettingsProvider settingsProvider,
        ILogger<ConfigCommandHandler>? logger = null)
        : base(logger)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "config",
        Description = "Manages application configuration",
        Syntax = "/config [show|open|reload]",
        Examples =
        [
            "/config",
            "/config show",
            "/config open",
            "/config reload"
        ],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["settings", "conf"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var action = command.Arguments.Count > 0 ? command.Arguments[0].ToLowerInvariant() : "show";

        return action switch
        {
            "show" => ShowConfigPath(),
            "open" => OpenConfigFile(),
            "reload" => ReloadConfig(context),
            _ => InvalidArguments("/config [show|open|reload]")
        };
    }

    private CommandResult ShowConfigPath()
    {
        var path = _settingsProvider.GetSettingsPath();
        var exists = _settingsProvider.SettingsFileExists();

        var sb = new StringBuilder();
        sb.AppendLine("## Configuration");
        sb.AppendLine();
        sb.AppendLine($"**Path:** `{path}`");
        sb.AppendLine($"**Exists:** {(exists ? "Yes" : "No")}");
        sb.AppendLine();

        if (exists)
        {
            var fileInfo = new FileInfo(path);
            sb.AppendLine($"**Size:** {fileInfo.Length:N0} bytes");
            sb.AppendLine($"**Modified:** {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        }
        sb.AppendLine();

        sb.AppendLine("**Commands:**");
        sb.AppendLine("- `/config open` - Open configuration file");
        sb.AppendLine("- `/config reload` - Reload configuration");

        return Success(sb.ToString());
    }

    private CommandResult OpenConfigFile()
    {
        var path = _settingsProvider.GetSettingsPath();

        if (!_settingsProvider.SettingsFileExists())
        {
            return new CommandResult
            {
                IsSuccess = false,
                Response = "## Configuration Not Found\n\nThe configuration file does not exist yet. It will be created when you save settings.",
                Status = CommandStatus.NotAvailable,
                CancelSend = true
            };
        }

        try
        {
            _logger.LogInformation("Opening configuration file: {Path}", path);

            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            process.Start();

            var sb = new StringBuilder();
            sb.AppendLine("## Opening Configuration");
            sb.AppendLine();
            sb.AppendLine($"**File:** `{path}`");
            sb.AppendLine();

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open configuration file");
            var sb = new StringBuilder();
            sb.AppendLine("## Failed to Open");
            sb.AppendLine();
            sb.AppendLine($"Could not open configuration file: {ex.Message}");
            sb.AppendLine();
            sb.AppendLine($"**Path:** `{path}`");
            sb.AppendLine("Open the file manually in your preferred editor.");
            return Error(sb.ToString());
        }
    }

    private CommandResult ReloadConfig(CommandContext context)
    {
        try
        {
            var settings = _settingsProvider.LoadSettingsAsync().Result;
            context.Settings = settings;

            _logger.LogInformation("Configuration reloaded");

            var sb = new StringBuilder();
            sb.AppendLine("## Configuration Reloaded");
            sb.AppendLine();
            sb.AppendLine("Settings have been reloaded from disk.");
            sb.AppendLine();
            sb.AppendLine("**Note:** Some changes may require restart to take effect.");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            return Error($"Failed to reload configuration: {ex.Message}");
        }
    }
}
