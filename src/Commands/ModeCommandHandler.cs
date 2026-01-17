using System.Text;
using Microsoft.Extensions.Logging;
using PairAdmin.Automation;

namespace PairAdmin.Commands;

public class ModeCommandHandler : CommandHandlerBase
{
    private readonly AutonomyManager? _autonomyManager;

    public ModeCommandHandler(ILogger<ModeCommandHandler>? logger = null, AutonomyManager? autonomyManager = null)
        : base(logger)
    {
        _autonomyManager = autonomyManager;
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "mode",
        Description = "Manages the AI autonomy mode",
        Syntax = "/mode [show|manual|auto|confirm]",
        Examples = new List<string> { "/mode", "/mode manual", "/mode auto", "/mode confirm" },
        Category = "Configuration",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = new List<string> { "autonomy" }
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (_autonomyManager == null)
            return Error("Autonomy manager not configured");

        var subcommand = command.Arguments.FirstOrDefault()?.ToLowerInvariant() ?? "show";

        return subcommand switch
        {
            "show" or "" => ShowCurrentMode(),
            "manual" => SetMode(AutonomyMode.Manual),
            "confirm" => SetMode(AutonomyMode.Confirm),
            "auto" => SetMode(AutonomyMode.Auto),
            _ => Error("Unknown mode: " + subcommand)
        };
    }

    private CommandResult ShowCurrentMode()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Autonomy Mode");
        sb.AppendLine();
        sb.AppendLine("**Current Mode:** " + _autonomyManager!.CurrentMode);
        sb.AppendLine();
        sb.AppendLine("### Available Modes");
        sb.AppendLine("- **manual**: AI suggests commands, you execute manually");
        sb.AppendLine("- **confirm**: AI suggests commands, you confirm before execution");
        sb.AppendLine("- **auto**: AI executes commands automatically");
        return Success(sb.ToString());
    }

    private CommandResult SetMode(AutonomyMode mode)
    {
        _autonomyManager!.SetMode(mode);
        var desc = mode switch
        {
            AutonomyMode.Manual => "AI will suggest commands for manual execution.",
            AutonomyMode.Confirm => "AI will ask confirmation before executing.",
            AutonomyMode.Auto => "AI will execute commands automatically!",
            _ => ""
        };
        var sb = new StringBuilder();
        sb.AppendLine("## Mode Changed: " + mode);
        sb.AppendLine(desc);
        if (mode == AutonomyMode.Auto)
            sb.AppendLine("**Warning:** Auto mode executes without confirmation.");
        return Success(sb.ToString());
    }
}
