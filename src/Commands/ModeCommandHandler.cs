using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ModeCommandHandler : CommandHandlerBase
{
    public ModeCommandHandler(ILogger<ModeCommandHandler>? logger = null)
        : base(logger)
    {
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "mode",
        Description = "Manages the AI autonomy mode",
        Syntax = "/mode [show|manual|auto|confirm]",
        Examples =
        [
            "/mode",
            "/mode manual",
            "/mode auto",
            "/mode confirm"
        ],
        Category = "Configuration",
        IsAvailable = false,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["autonomy"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        return NotAvailable("The /mode command requires Phase 5 (AutonomyManager) implementation. " +
            "This feature is currently blocked pending Windows-specific PuTTY integration.");
    }
}
