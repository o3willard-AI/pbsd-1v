namespace PairAdmin.Security;

/// <summary>
/// Result of command validation
/// </summary>
public class CommandValidationResult
{
    /// <summary>
    /// Whether the command is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Reason for validation failure
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Status of the validation
    /// </summary>
    public CommandValidationStatus Status { get; set; }

    /// <summary>
    /// Warnings about the command
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Additional details about the validation
    /// </summary>
    public List<string> Details { get; set; } = new();

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static CommandValidationResult Success() => new()
    {
        IsValid = true,
        Status = CommandValidationStatus.Allowed
    };

    /// <summary>
    /// Creates a forbidden result
    /// </summary>
    public static CommandValidationResult Forbidden(string reason) => new()
    {
        IsValid = false,
        FailureReason = reason,
        Status = CommandValidationStatus.Forbidden
    };

    /// <summary>
    /// Creates a warning result
    /// </summary>
    public static CommandValidationResult Warning(string reason, List<string>? details = null) => new()
    {
        IsValid = true,
        FailureReason = reason,
        Status = CommandValidationStatus.AllowedWithWarning,
        Details = details ?? new List<string>()
    };

    /// <summary>
    /// Creates a result requiring confirmation
    /// </summary>
    public static CommandValidationResult RequiresConfirmation(string reason) => new()
    {
        IsValid = false,
        FailureReason = reason,
        Status = CommandValidationStatus.RequiresConfirmation
    };
}

/// <summary>
/// Status of command validation
/// </summary>
public enum CommandValidationStatus
{
    /// <summary>
    /// Command is allowed
    /// </summary>
    Allowed,

    /// <summary>
    /// Command is allowed with a warning
    /// </summary>
    AllowedWithWarning,

    /// <summary>
    /// Command is forbidden
    /// </summary>
    Forbidden,

    /// <summary>
    /// Command requires user confirmation
    /// </summary>
    RequiresConfirmation
}
