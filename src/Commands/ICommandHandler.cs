using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

/// <summary>
/// Interface for command handlers
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Command metadata
    /// </summary>
    CommandMetadata Metadata { get; }

    /// <summary>
    /// Executes the command
    /// </summary>
    /// <param name="context">Command execution context</param>
    /// <param name="command">Parsed command</param>
    /// <returns>Command result</returns>
    CommandResult Execute(CommandContext context, ParsedCommand command);

    /// <summary>
    /// Gets help text for this command
    /// </summary>
    string GetHelpText();

    /// <summary>
    /// Validates that the command can be executed
    /// </summary>
    bool CanExecute(CommandContext context);
}

/// <summary>
/// Base class for command handlers with common functionality
/// </summary>
public abstract class CommandHandlerBase : ICommandHandler
{
    protected readonly ILogger _logger;

    /// <summary>
    /// Command metadata
    /// </summary>
    public abstract CommandMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    protected CommandHandlerBase(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Executes the command
    /// </summary>
    public abstract CommandResult Execute(CommandContext context, ParsedCommand command);

    /// <summary>
    /// Gets help text for this command
    /// </summary>
    public virtual string GetHelpText()
    {
        return Metadata.GetHelpText();
    }

    /// <summary>
    /// Validates that the command can be executed
    /// </summary>
    public virtual bool CanExecute(CommandContext context)
    {
        return true;
    }

    /// <summary>
    /// Creates a success result
    /// </summary>
    protected CommandResult Success(string response, bool cancelSend = true)
    {
        return CommandResult.Success(response, cancelSend);
    }

    /// <summary>
    /// Creates an invalid arguments result
    /// </summary>
    protected CommandResult InvalidArguments(string syntax)
    {
        return CommandResult.InvalidArguments(Metadata.Name, syntax);
    }

    /// <summary>
    /// Creates an error result
    /// </summary>
    protected CommandResult Error(string message)
    {
        return CommandResult.Error(Metadata.Name, message);
    }

    /// <summary>
    /// Creates a not available result
    /// </summary>
    protected CommandResult NotAvailable(string reason)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Response = $"Command '/{Metadata.Name}' is not available: {reason}",
            Status = CommandStatus.NotAvailable,
            CancelSend = true
        };
    }
}
