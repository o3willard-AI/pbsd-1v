using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ClearCommandHandler : CommandHandlerBase
{
    public ClearCommandHandler(ILogger<ClearCommandHandler>? logger = null)
        : base(logger)
    {
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "clear",
        Description = "Clears chat history and resets the conversation",
        Syntax = "/clear [all|context|messages]",
        Examples =
        [
            "/clear",
            "/clear messages",
            "/clear all"
        ],
        Category = "Core",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["cls", "reset"]
    }

    public override bool CanExecute(CommandContext context)
    {
        return context.ChatPane != null || context.Messages.Count > 0;
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var scope = command.Arguments.Count > 0 ? command.Arguments[0].ToLowerInvariant() : "all";

        return scope switch
        {
            "all" => ClearAll(context),
            "context" => ClearContext(context),
            "messages" => ClearMessages(context),
            _ => InvalidArguments("/clear [all|context|messages]")
        };
    }

    private CommandResult ClearAll(CommandContext context)
    {
        try
        {
            context.Messages.Clear();
            context.ContextManager?.Clear();

            _logger.LogInformation("Cleared all chat history and context");

            return Success("## Chat Cleared\n\nAll chat history and context have been cleared. Starting fresh!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear all");
            return Error(ex.Message);
        }
    }

    private CommandResult ClearContext(CommandContext context)
    {
        if (context.ContextManager == null)
        {
            return NotAvailable("Context manager not available");
        }

        try
        {
            context.ContextManager.Clear();
            _logger.LogInformation("Cleared context window");

            return Success("## Context Cleared\n\nContext window has been cleared. The AI will have minimal history for the next message.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear context");
            return Error(ex.Message);
        }
    }

    private CommandResult ClearMessages(CommandContext context)
    {
        try
        {
            var count = context.Messages.Count;
            context.Messages.Clear();
            _logger.LogInformation("Cleared {Count} messages", count);

            return Success($"## Messages Cleared\n\nCleared {count} messages from chat history.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear messages");
            return Error(ex.Message);
        }
    }
}
