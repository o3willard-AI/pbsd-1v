using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

public class HelpCommandHandler : CommandHandlerBase
{
    private readonly CommandRegistry _registry;

    public HelpCommandHandler(CommandRegistry registry, ILogger<HelpCommandHandler>? logger = null)
        : base(logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "help",
        Description = "Displays help information for commands",
        Syntax = "/help [command]",
        Examples =
        [
            "/help",
            "/help context",
            "/help model"
        ],
        Category = "Core",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["h", "?"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (command.Arguments.Count == 0)
        {
            return ShowCommandList();
        }

        return ShowCommandHelp(command.Arguments[0]);
    }

    private CommandResult ShowCommandList()
    {
        var helpList = _registry.GetHelpList();
        var sb = new StringBuilder();

        sb.AppendLine(helpList);
        sb.AppendLine();
        sb.AppendLine("**Tips:**");
        sb.AppendLine("- Type `/help <command>` for detailed help");
        sb.AppendLine("- Use `/clear` to reset the conversation");
        sb.AppendLine("- Use `/context` to manage context window");

        return Success(sb.ToString());
    }

    private CommandResult ShowCommandHelp(string commandName)
    {
        var metadata = _registry.GetMetadata(commandName);

        if (metadata == null)
        {
            var suggestions = GetSimilarCommands(commandName);
            var sb = new StringBuilder();

            sb.AppendLine($"Command '/{commandName}' not found.");
            sb.AppendLine();

            if (suggestions.Count > 0)
            {
                sb.AppendLine("Did you mean:");
                foreach (var suggestion in suggestions.Take(3))
                {
                    sb.AppendLine($"  - /{suggestion}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Use `/help` to see all available commands.");

            return new CommandResult
            {
                IsSuccess = false,
                Response = sb.ToString(),
                Status = CommandStatus.NotFound,
                CancelSend = true
            };
        }

        return Success(metadata.GetHelpText());
    }

    private List<string> GetSimilarCommands(string query)
    {
        var results = _registry.Search(query);
        return results.Select(r => r.Name).ToList();
    }
}
