using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class SaveLogCommandHandler : CommandHandlerBase
{
    private readonly string _defaultLogDirectory;

    public SaveLogCommandHandler(
        ILogger<SaveLogCommandHandler>? logger = null,
        string? defaultLogDirectory = null)
        : base(logger)
    {
        _defaultLogDirectory = defaultLogDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PairAdmin",
            "Logs");
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "save-log",
        Description = "Exports the session log to a file",
        Syntax = "/save-log [path] [--format text|json]",
        Examples =
        [
            "/save-log",
            "/save-log session.log",
            "/save-log --format json out.json",
            "/save-log /home/user/logs/session.txt"
        ],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["log", "save"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var format = command.Flags.GetValueOrDefault("format", "text");
        var path = command.Arguments.Count > 0 ? command.Arguments[0] : GenerateDefaultPath();

        return ExportLog(context, path, format);
    }

    private string GenerateDefaultPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"pairadmin_session_{timestamp}.txt";
        return Path.Combine(_defaultLogDirectory, fileName);
    }

    private CommandResult ExportLog(CommandContext context, string path, string format)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var content = format.Equals("json", StringComparison.OrdinalIgnoreCase)
                ? GenerateJsonLog(context)
                : GenerateTextLog(context);

            File.WriteAllText(path, content);

            _logger.LogInformation("Session log saved to {Path}", path);

            var sb = new StringBuilder();
            sb.AppendLine("## Session Log Saved");
            sb.AppendLine();
            sb.AppendLine($"**File:** `{path}`");
            sb.AppendLine($"**Format:** {format.ToUpper()}");
            sb.AppendLine($"**Size:** {content.Length:N0} bytes");
            sb.AppendLine($"**Messages:** {context.Messages.Count}");
            sb.AppendLine();

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session log");
            return Error($"Failed to save log: {ex.Message}");
        }
    }

    private string GenerateTextLog(CommandContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== PairAdmin Session Log ===");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Messages: {context.Messages.Count}");
        sb.AppendLine();

        foreach (var message in context.Messages)
        {
            sb.AppendLine("---");
            sb.AppendLine($"Role: {message.Role}");
            sb.AppendLine($"Timestamp: {message.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateJsonLog(CommandContext context)
    {
        var logData = new
        {
            GeneratedAt = DateTime.Now.ToString("o"),
            MessageCount = context.Messages.Count,
            Messages = context.Messages.Select(m => new
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp.ToString("o")
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(logData, options);
    }
}
