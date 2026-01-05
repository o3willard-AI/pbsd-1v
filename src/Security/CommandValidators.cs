using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security;

/// <summary>
/// Interface for command validators
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// Gets the validator name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Validates a command
    /// </summary>
    CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel);
}

/// <summary>
/// Base class for command validators
/// </summary>
public abstract class CommandValidatorBase : ICommandValidator
{
    protected readonly SecurityPolicy _policy;
    protected readonly ILogger? _logger;

    public abstract string Name { get; }

    protected CommandValidatorBase(SecurityPolicy policy, ILogger? logger = null)
    {
        _policy = policy;
        _logger = logger;
    }

    public abstract CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel);

    protected static string ExtractCommandName(string command)
    {
        var trimmed = command.TrimStart('/');
        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0
            ? trimmed.Substring(0, spaceIndex).ToLowerInvariant()
            : trimmed.ToLowerInvariant();
    }
}

/// <summary>
/// Validates against a whitelist
/// </summary>
public class WhitelistValidator : CommandValidatorBase
{
    public override string Name => "WhitelistValidator";

    public WhitelistValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        if (_policy.Mode != ValidationMode.Whitelist)
        {
            return CommandValidationResult.Success();
        }

        var commandName = ExtractCommandName(command);

        if (_policy.WhitelistedCommands.Contains(commandName))
        {
            return CommandValidationResult.Success();
        }

        return CommandValidationResult.Forbidden(
            $"Command '/{commandName}' is not in the allowed list");
    }
}

/// <summary>
/// Validates against a blacklist
/// </summary>
public class BlacklistValidator : CommandValidatorBase
{
    public override string Name => "BlacklistValidator";

    public BlacklistValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        var commandName = ExtractCommandName(command);

        if (_policy.BlacklistedCommands.Contains(commandName))
        {
            _logger?.LogWarning("Blacklisted command attempted: {Command}", commandName);
            return CommandValidationResult.Forbidden(
                $"Command '/{commandName}' is not allowed");
        }

        return CommandValidationResult.Success();
    }
}

/// <summary>
/// Validates privilege levels
/// </summary>
public class PrivilegeValidator : CommandValidatorBase
{
    public override string Name => "PrivilegeValidator";

    public PrivilegeValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        var commandName = ExtractCommandName(command);

        if (_policy.CommandPrivilegeRequirements.TryGetValue(
            commandName, out var requiredLevel))
        {
            if (privilegeLevel < requiredLevel)
            {
                return CommandValidationResult.Forbidden(
                    $"Command '/{commandName}' requires {requiredLevel} privileges");
            }
        }

        return CommandValidationResult.Success();
    }
}

/// <summary>
/// Validates command length
/// </summary>
public class LengthValidator : CommandValidatorBase
{
    public override string Name => "LengthValidator";

    public LengthValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        if (command.Length > _policy.MaxCommandLength)
        {
            return CommandValidationResult.Forbidden(
                $"Command exceeds maximum length of {_policy.MaxCommandLength} characters");
        }

        return CommandValidationResult.Success();
    }
}

/// <summary>
/// Validates command category
/// </summary>
public class CategoryValidator : CommandValidatorBase
{
    public override string Name => "CategoryValidator";

    public CategoryValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        if (_policy.AllowedCategories.Count == 0)
        {
            return CommandValidationResult.Success();
        }

        var commandName = ExtractCommandName(command);
        var details = context.Settings;

        return CommandValidationResult.Success();
    }
}
