using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class ContextCommandHandler : CommandHandlerBase
{
    public ContextCommandHandler(ILogger<ContextCommandHandler>? logger = null)
        : base(logger)
    {
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "context",
        Description = "Manages the context window size",
        Syntax = "/context [show|<lines>|--percentage <0-100>|--auto]",
        Examples =
        [
            "/context",
            "/context 100",
            "/context 50%",
            "/context --percentage 75",
            "/context --auto"
        ],
        Category = "Core",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["ctx", "window"]
    }

    public override bool CanExecute(CommandContext context)
    {
        return context.ContextManager != null;
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (context.ContextManager == null)
        {
            return NotAvailable("Context manager not available");
        }

        if (command.Arguments.Count == 0 && command.Flags.Count == 0)
        {
            return ShowCurrentContext(context);
        }

        if (command.Flags.TryGetValue("auto", out var autoValue))
        {
            return SetAutoMode(context);
        }

        if (command.Flags.TryGetValue("percentage", out var percentageValue))
        {
            return SetPercentage(context, percentageValue);
        }

        if (command.Arguments.Count > 0)
        {
            var arg = command.Arguments[0].ToLowerInvariant();

            if (arg == "show")
            {
                return ShowCurrentContext(context);
            }

            if (arg.EndsWith("%"))
            {
                var percentage = arg.TrimEnd('%');
                if (int.TryParse(percentage, out var pct))
                {
                    return SetPercentage(context, pct.ToString());
                }
                return InvalidArguments("/context <lines> or /context --percentage <0-100>");
            }

            if (int.TryParse(arg, out var lines))
            {
                return SetLines(context, lines);
            }
        }

        return InvalidArguments("/context [show|<lines>|--percentage <0-100>|--auto]");
    }

    private CommandResult ShowCurrentContext(CommandContext context)
    {
        var manager = context.ContextManager;
        var settings = context.Settings;

        var sb = new StringBuilder();

        sb.AppendLine("## Context Window Settings");
        sb.AppendLine();
        sb.AppendLine($"**Current Policy:** {manager.Policy}");
        sb.AppendLine($"**Max Lines:** {manager.MaxLines}");
        sb.AppendLine($"**Current Lines:** {manager.CurrentLines}");
        sb.AppendLine($"**Current Tokens:** {manager.CurrentTokens:N0}");
        sb.AppendLine();

        sb.AppendLine("**Available Policies:**");
        sb.AppendLine("- `Auto` - Use model defaults");
        sb.AppendLine("- `Fixed` - Exact line limit");
        sb.AppendLine("- `Percentage` - Percentage of max");

        return Success(sb.ToString());
    }

    private CommandResult SetLines(CommandContext context, int lines)
    {
        if (lines < 1 || lines > 10000)
        {
            return Error("Lines must be between 1 and 10,000");
        }

        try
        {
            context.ContextManager.MaxLines = lines;
            context.ContextManager.Policy = ContextSizePolicy.Fixed;

            _logger.LogInformation("Set context max lines to {Lines}", lines);

            var sb = new StringBuilder();
            sb.AppendLine("## Context Updated");
            sb.AppendLine();
            sb.AppendLine($"Context window set to **{lines} lines**.");
            sb.AppendLine($"Current usage: {context.ContextManager.CurrentLines}/{lines} lines");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set context lines");
            return Error(ex.Message);
        }
    }

    private CommandResult SetPercentage(CommandContext context, string percentageValue)
    {
        if (!int.TryParse(percentageValue, out var percentage) || percentage < 0 || percentage > 100)
        {
            return Error("Percentage must be between 0 and 100");
        }

        try
        {
            context.ContextManager.SetPercentage(percentage);
            context.ContextManager.Policy = ContextSizePolicy.Percentage;

            _logger.LogInformation("Set context percentage to {Percentage}%", percentage);

            var sb = new StringBuilder();
            sb.AppendLine("## Context Updated");
            sb.AppendLine();
            sb.AppendLine($"Context window set to **{percentage}%** of maximum.");
            sb.AppendLine($"Actual lines: ~{context.ContextManager.MaxLines * percentage / 100} lines");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set context percentage");
            return Error(ex.Message);
        }
    }

    private CommandResult SetAutoMode(CommandContext context)
    {
        try
        {
            context.ContextManager.Policy = ContextSizePolicy.Auto;

            _logger.LogInformation("Set context policy to Auto");

            var sb = new StringBuilder();
            sb.AppendLine("## Context Updated");
            sb.AppendLine();
            sb.AppendLine("Context policy set to **Auto**.");
            sb.AppendLine("The system will automatically determine the optimal context size based on the LLM model.");

            return Success(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set auto mode");
            return Error(ex.Message);
        }
    }
}
