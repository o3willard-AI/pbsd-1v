using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class QuitCommandHandler : CommandHandlerBase
{
    public QuitCommandHandler(ILogger<QuitCommandHandler>? logger = null)
        : base(logger)
    {
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "quit",
        Description = "Closes the application",
        Syntax = "/quit [--force]",
        Examples =
        [
            "/quit",
            "/quit --force"
        ],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["exit", "close", "q"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var force = command.Flags.ContainsKey("force");

        if (!force)
        {
            return RequestConfirmation();
        }

        return Shutdown();
    }

    private CommandResult RequestConfirmation()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Confirm Quit");
        sb.AppendLine();
        sb.AppendLine("Are you sure you want to close PairAdmin?");
        sb.AppendLine();
        sb.AppendLine("**Note:** This command requires UI integration to actually close the window.");
        sb.AppendLine();
        sb.AppendLine("_To close the application, use `/quit --force` or close the window manually._");

        return new CommandResult
        {
            IsSuccess = true,
            Response = sb.ToString(),
            Status = CommandStatus.RequiresConfirmation,
            CancelSend = true
        };
    }

    private CommandResult Shutdown()
    {
        _logger.LogInformation("Quit command invoked");

        var sb = new StringBuilder();
        sb.AppendLine("## Closing PairAdmin");
        sb.AppendLine();
        sb.AppendLine("PairAdmin is shutting down...");
        sb.AppendLine();
        sb.AppendLine("_Note: This command requires UI integration to actually close the window._");
        sb.AppendLine("Close the window manually if this message persists.");

        return Success(sb.ToString());
    }
}
