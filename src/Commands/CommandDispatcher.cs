using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

/// <summary>
/// Command dispatcher that routes commands to handlers
/// </summary>
public class CommandDispatcher
{
    private readonly CommandRegistry _registry;
    private readonly SlashCommandParser _parser;
    private readonly ILogger<CommandDispatcher> _logger;

    /// <summary>
    /// Event raised when a command is executed
    /// </summary>
    public event EventHandler<CommandResult>? CommandExecuted;

    /// <summary>
    /// Event raised when a command is not found
    /// </summary>
    public event EventHandler<string>? CommandNotFound;

    /// <summary>
    /// Event raised when a command has an error
    /// </summary>
    public event EventHandler<CommandResult>? CommandError;

    /// <summary>
    /// Initializes a new instance of CommandDispatcher
    /// </summary>
    public CommandDispatcher(
        CommandRegistry registry,
        SlashCommandParser parser,
        ILogger<CommandDispatcher> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("CommandDispatcher initialized");
    }

    /// <summary>
    /// Initializes a new instance with default implementations
    /// </summary>
    public CommandDispatcher(ILogger<CommandDispatcher> logger) : this(
        new CommandRegistry(logger),
        new SlashCommandParser(logger),
        logger)
    {
    }

    /// <summary>
    /// Executes a command from input text
    /// </summary>
    public CommandResult Execute(string input, CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CommandResult.Error("empty", "No command provided");
        }

        var trimmed = input.TrimStart();

        if (!trimmed.StartsWith("/"))
        {
            _logger.LogDebug("Input does not start with /, not a slash command");
            return CommandResult.Success(string.Empty, cancelSend: false);
        }

        _logger.LogInformation($"Executing command: {input}");

        var parsed = _parser.Parse(input);

        if (!parsed.IsValid)
        {
            _logger.LogWarning($"Failed to parse command: {parsed.ParseError}");
            var result = CommandResult.Error(parsed.CommandName, parsed.ParseError ?? "Invalid command format");
            CommandError?.Invoke(this, result);
            return result;
        }

        var handler = _registry.GetHandler(parsed.CommandName);

        if (handler == null)
        {
            var suggestions = _parser.GetSuggestions(trimmed, _registry.RegisteredCommands.Values.ToList());
            var response = BuildNotFoundResponse(parsed.CommandName, suggestions);
            var result = CommandResult.NotFound(parsed.CommandName);
            result.AdditionalData = new Dictionary<string, object>
            {
                { "Suggestions", suggestions }
            };

            _logger.LogWarning($"Command not found: /{parsed.CommandName}");
            CommandNotFound?.Invoke(this, parsed.CommandName);
            CommandExecuted?.Invoke(this, result);

            return result;
        }

        var metadata = handler.Metadata;

        if (!handler.CanExecute(context))
        {
            var result = new CommandResult
            {
                IsSuccess = false,
                Response = $"Command '/{metadata.Name}' is not available in the current context.",
                Status = CommandStatus.NotAvailable,
                CancelSend = true
            };

            _logger.LogWarning($"Command not available: /{metadata.Name}");
            CommandExecuted?.Invoke(this, result);

            return result;
        }

        if (!_parser.ValidateArguments(parsed, metadata))
        {
            var result = _parser.InvalidArguments(parsed.CommandName, metadata.Syntax);
            _logger.LogWarning($"Invalid arguments for /{metadata.Name}");
            CommandError?.Invoke(this, result);
            CommandExecuted?.Invoke(this, result);

            return result;
        }

        try
        {
            var result = handler.Execute(context, parsed);
            _logger.LogInformation($"Command /{metadata.Name} executed: success={result.IsSuccess}");

            CommandExecuted?.Invoke(this, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing command /{metadata.Name}");
            var result = CommandResult.Error(metadata.Name, ex.Message);
            CommandError?.Invoke(this, result);
            CommandExecuted?.Invoke(this, result);

            return result;
        }
    }

    /// <summary>
    /// Gets the command registry
    /// </summary>
    public CommandRegistry GetRegistry()
    {
        return _registry;
    }

    /// <summary>
    /// Gets the command parser
    /// </summary>
    public SlashCommandParser GetParser()
    {
        return _parser;
    }

    private string BuildNotFoundResponse(string commandName, System.Collections.Generic.List<string> suggestions)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Command '/{commandName}' not found.");
        sb.AppendLine();

        if (suggestions.Count > 0)
        {
            sb.AppendLine("Did you mean:");
            foreach (var suggestion in suggestions.Take(5))
            {
                sb.AppendLine($"  - {suggestion}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Use `/help` to see all available commands.");

        return sb.ToString();
    }

    /// <summary>
    /// Gets suggestions for a partial command
    /// </summary>
    public System.Collections.Generic.List<string> GetSuggestions(string partial)
    {
        return _parser.GetSuggestions(partial, _registry.RegisteredCommands.Values.ToList());
    }
}
