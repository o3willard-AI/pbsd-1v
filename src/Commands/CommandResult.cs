namespace PairAdmin.Commands;

/// <summary>
/// Result of command execution
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Whether the command was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Response message to display
    /// </summary>
    public string Response { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Status of command execution
    /// </summary>
    public CommandStatus Status { get; set; }

    /// <summary>
    /// Whether to suppress sending to LLM
    /// </summary>
    public bool CancelSend { get; set; }

    /// <summary>
    /// Additional data from command
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CommandResult Success(string response, bool cancelSend = true)
    {
        return new CommandResult
        {
            IsSuccess = true,
            Response = response,
            Status = CommandStatus.Executed,
            CancelSend = cancelSend
        };
    }

    /// <summary>
    /// Creates a not found result
    /// </summary>
    public static CommandResult NotFound(string command)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Response = $"Command '{command}' not found.",
            ErrorMessage = $"Command '{command}' not found.",
            Status = CommandStatus.NotFound,
            CancelSend = true
        };
    }

    /// <summary>
    /// Creates an invalid arguments result
    /// </summary>
    public static CommandResult InvalidArguments(string command, string syntax)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Response = $"Invalid arguments for '{command}'. Usage: {syntax}",
            ErrorMessage = "Invalid arguments",
            Status = CommandStatus.InvalidArguments,
            CancelSend = true
        };
    }

    /// <summary>
    /// Creates an error result
    /// </summary>
    public static CommandResult Error(string command, string errorMessage)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Response = $"Error executing '{command}': {errorMessage}",
            ErrorMessage = errorMessage,
            Status = CommandStatus.Error,
            CancelSend = true
        };
    }

    /// <summary>
    /// Creates a cancelled result
    /// </summary>
    public static CommandResult Cancelled(string command)
    {
        return new CommandResult
        {
            IsSuccess = false,
            Response = $"Command '{command}' was cancelled.",
            Status = CommandStatus.Cancelled,
            CancelSend = true
        };
    }
}

/// <summary>
/// Status of command execution
/// </summary>
public enum CommandStatus
{
    /// <summary>
    /// Command executed successfully
    /// </summary>
    Executed,

    /// <summary>
    /// Command not found
    /// </summary>
    NotFound,

    /// <summary>
    /// Invalid arguments provided
    /// </summary>
    InvalidArguments,

    /// <summary>
    /// Error during execution
    /// </summary>
    Error,

    /// <summary>
    /// Command was cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Command requires confirmation
    /// </summary>
    RequiresConfirmation,

    /// <summary>
    /// Command is not available in current context
    /// </summary>
    NotAvailable
}
