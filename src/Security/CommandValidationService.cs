using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security;

/// <summary>
/// Service for validating commands before execution
/// </summary>
public class CommandValidationService
{
    private readonly SecurityPolicy _policy;
    private readonly List<ICommandValidator> _validators;
    private readonly ILogger<CommandValidationService>? _logger;
    private readonly ConcurrentDictionary<string, CommandValidationResult> _cache;

    /// <summary>
    /// Creates a default validation service
    /// </summary>
    public static CommandValidationService CreateDefault(ILogger<CommandValidationService>? logger = null)
    {
        var policy = SecurityPolicy.CreateDefault();
        return new CommandValidationService(policy, logger);
    }

    /// <summary>
    /// Creates a strict validation service
    /// </summary>
    public static CommandValidationService CreateStrict(ILogger<CommandValidationService>? logger = null)
    {
        var policy = SecurityPolicy.CreateStrict();
        return new CommandValidationService(policy, logger);
    }

    public CommandValidationService(
        SecurityPolicy policy,
        ILogger<CommandValidationService>? logger = null)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CommandValidationResult>();

        _validators = new List<ICommandValidator>
        {
            new LengthValidator(policy, logger),
            new WhitelistValidator(policy, logger),
            new BlacklistValidator(policy, logger),
            new PrivilegeValidator(policy, logger),
            new DangerousCommandValidator(policy, logger)
        };

        _logger?.LogInformation("CommandValidationService initialized with {Count} validators", _validators.Count);
    }

    /// <summary>
    /// Validates a command
    /// </summary>
    public CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel? currentPrivilegeLevel = null)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return CommandValidationResult.Forbidden("Empty command");
        }

        if (_policy.Mode == ValidationMode.Disabled)
        {
            return CommandValidationResult.Success();
        }

        var privilege = currentPrivilegeLevel ?? _policy.DefaultPrivilegeLevel;

        foreach (var validator in _validators)
        {
            try
            {
                var result = validator.Validate(command, context, privilege);

                if (!result.IsValid)
                {
                    _logger?.LogWarning(
                        "Command {Command} rejected by {Validator}: {Reason}",
                        command, validator.Name, result.FailureReason);

                    if (result.Status == CommandValidationStatus.AllowedWithWarning)
                    {
                        _logger?.LogWarning(
                            "Command {Command} has warning from {Validator}: {Reason}",
                            command, validator.Name, result.FailureReason);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Validator {Validator} threw exception", validator.Name);
            }
        }

        _logger?.LogDebug("Command {Command} validated successfully", command);
        return CommandValidationResult.Success();
    }

    /// <summary>
    /// Checks if a command requires elevated privileges
    /// </summary>
    public bool RequiresElevatedPrivilege(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var commandName = ExtractCommandName(command);

        return _policy.CommandPrivilegeRequirements.TryGetValue(
            commandName, out var level) && level > PrivilegeLevel.Standard;
    }

    /// <summary>
    /// Checks if a command is blacklisted
    /// </summary>
    public bool IsBlacklisted(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var commandName = ExtractCommandName(command);
        return _policy.BlacklistedCommands.Contains(commandName);
    }

    /// <summary>
    /// Checks if a command is whitelisted
    /// </summary>
    public bool IsWhitelisted(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        var commandName = ExtractCommandName(command);
        return _policy.WhitelistedCommands.Contains(commandName);
    }

    /// <summary>
    /// Gets the security policy
    /// </summary>
    public SecurityPolicy GetPolicy() => _policy;

    /// <summary>
    /// Updates the security policy at runtime
    /// </summary>
    public void UpdatePolicy(Action<SecurityPolicy> update)
    {
        update(_policy);
        _cache.Clear();
        _logger?.LogInformation("Security policy updated");
    }

    /// <summary>
    /// Clears the validation cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger?.LogDebug("Validation cache cleared");
    }

    private static string ExtractCommandName(string command)
    {
        var trimmed = command.TrimStart('/');
        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0
            ? trimmed.Substring(0, spaceIndex).ToLowerInvariant()
            : trimmed.ToLowerInvariant();
    }
}
