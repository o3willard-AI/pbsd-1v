using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ExportCommandHandler : CommandHandlerBase
{
    public ExportCommandHandler(ILogger<ExportCommandHandler>? logger = null)
        : base(logger)
    {
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "export",
        Description = "Exports chat history to clipboard or file",
        Syntax = "/export [--format text|json] [--copy]",
        Examples =
        [
            "/export",
            "/export --format json",
            "/export --copy",
            "/export --format json --copy"
        ],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["save-chat", "download"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var copy = command.Flags.ContainsKey("copy");
        var format = command.Flags.GetValueOrDefault("format", "text");

        if (copy)
        {
            return CopyToClipboard(context, format);
        }

        return ExportChat(context, format);
    }

    private CommandResult ExportChat(CommandContext context, string format)
    {
        if (context.Messages.Count == 0)
        {
            return Success("## No Messages\n\nThere are no messages to export. Start a conversation first!");
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = format.Equals("json", StringComparison.OrdinalIgnoreCase) ? "json" : "txt";
            var fileName = $"pairadmin_export_{timestamp}.{extension}";

            var content = format.Equals("json", StringComparison.OrdinalIgnoreCase)
                ? GenerateJsonExport(context)
                : GenerateTextExport(context);

            var exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PairAdmin",
                "Exports",
                fileName);

            var directory = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(exportPath, content);

            _logger.LogInformation("Chat exported to {Path}", exportPath);

            var sb = new StringBuilder();
            sb.AppendLine("## Chat Exported");
            sb.AppendLine();
            sb.AppendLine($"**File:** `{exportPath}`");
            sb.AppendLine($"**Format:** {format.ToUpper()}");
            sb.AppendLine($"**Messages:** {context.Messages.Count}");
            sb.AppendLine();
            sb.AppendLine($"Use `/export --copy` to copy to clipboard instead.");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export chat");
            return Error($"Failed to export: {ex.Message}");
        }
    }

    private CommandResult CopyToClipboard(CommandContext context, string format)
    {
        if (context.Messages.Count == 0)
        {
            return Success("## No Messages\n\nThere are no messages to copy. Start a conversation first!");
        }

        try
        {
            var content = format.Equals("json", StringComparison.OrdinalIgnoreCase)
                ? GenerateJsonExport(context)
                : GenerateTextExport(context);

            try
            {
                System.Windows.Clipboard.SetText(content);
                _logger.LogInformation("Chat copied to clipboard");
            }
            catch
            {
                _logger.LogWarning("Failed to copy to clipboard, showing content instead");
            }

            var sb = new StringBuilder();
            sb.AppendLine("## Copied to Clipboard");
            sb.AppendLine();
            sb.AppendLine($"**Format:** {format.ToUpper()}");
            sb.AppendLine($"**Messages:** {context.Messages.Count}");
            sb.AppendLine();

            if (!System.Windows.Clipboard.ContainsText())
            {
                sb.AppendLine("_Note: Clipboard access failed. Content preview:_");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                sb.AppendLine("```");
            }

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
            return Error($"Failed to copy: {ex.Message}");
        }
    }

    private string GenerateTextExport(CommandContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== PairAdmin Chat Export ===");
        sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Messages: {context.Messages.Count}");
        sb.AppendLine();

        foreach (var message in context.Messages)
        {
            sb.AppendLine("---");
            sb.AppendLine($"[{message.Timestamp:yyyy-MM-dd HH:mm:ss}] {message.Role.ToUpper()}");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateJsonExport(CommandContext context)
    {
        var exportData = new
        {
            ExportedAt = DateTime.Now.ToString("o"),
            MessageCount = context.Messages.Count,
            Messages = context.Messages.Select(m => new
            {
                Timestamp = m.Timestamp.ToString("o"),
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(exportData, options);
    }
}
