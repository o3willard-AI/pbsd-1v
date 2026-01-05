# Task 7.2 Specification: Command Validation

## Task: Implement Command Validation Service

**Phase:** Phase 7: Security  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.1 (CommandDispatcher) complete

---

## Description

Implement a command validation service that validates user commands before execution. This provides an additional security layer to prevent dangerous commands from being executed and allows for privilege-based access control.

---

## Deliverables

### 1. ICommandValidator.cs
Command validation interface:
- Validation rules interface
- Composite validator
- Validation result types

### 2. CommandValidationService.cs
Core validation service:
- Command whitelisting/blacklisting
- Pattern-based validation
- Privilege level validation

### 3. SecurityPolicy.cs
Security policy definitions:
- Privilege levels
- Command restrictions
- Security policy configuration

---

## Requirements

### Functional Requirements

#### Command Whitelist/Blacklist
| Requirement | Description |
|-------------|-------------|
| Whitelist Mode | Only allow specified commands |
| Blacklist Mode | Block specified commands |
| Pattern Matching | Support regex patterns |
| Category Restrictions | Restrict entire categories |

#### Privilege Levels
| Level | Description |
|-------|-------------|
| Standard | Normal user commands |
| Elevated | Commands requiring sudo/admin |
| Restricted | Limited command set |

#### Dangerous Command Detection
| Requirement | Description |
|-------------|-------------|
| Shell Injection | Detect shell metacharacters |
| Path Traversal | Detect ../ in paths |
| Command Chaining | Detect ; & && \| |
| Dangerous Patterns | Detect rm, format, etc. |

### Non-Functional Requirements

1. **Performance**
   - Fast validation (< 1ms)
   - Caching support

2. **Extensibility**
   - Custom validation rules
   - Pluggable validators

3. **Configurability**
   - Policy configuration
   - Runtime changes

---

## Implementation

### ICommandValidator Interface

```csharp
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
        CommandContext context,
        PrivilegeLevel privilegeLevel);
}
```

### Validation Result

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Result of command validation
/// </summary>
public class CommandValidationResult
{
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public CommandValidationStatus Status { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Details { get; set; } = new();

    public static CommandValidationResult Success() => new()
    {
        IsValid = true,
        Status = CommandValidationStatus.Allowed
    };

    public static CommandValidationResult Forbidden(string reason) => new()
    {
        IsValid = false,
        FailureReason = reason,
        Status = CommandValidationStatus.Forbidden
    };

    public static CommandValidationResult Warning(string reason) => new()
    {
        IsValid = true,
        FailureReason = reason,
        Status = CommandValidationStatus.AllowedWithWarning
    };
}

public enum CommandValidationStatus
{
    Allowed,
    AllowedWithWarning,
    Forbidden,
    RequiresConfirmation
}
```

### Security Policy

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Security policy for command validation
/// </summary>
public class SecurityPolicy
{
    /// <summary>
    /// Validation mode
    /// </summary>
    public ValidationMode Mode { get; set; } = ValidationMode.Blacklist;

    /// <summary>
    /// Default privilege level
    /// </summary>
    public PrivilegeLevel DefaultPrivilegeLevel { get; set; } = PrivilegeLevel.Standard;

    /// <summary>
    /// Whitelisted commands
    /// </summary>
    public HashSet<string> WhitelistedCommands { get; set; } = new();

    /// <summary>
    /// Blacklisted commands
    /// </summary>
    public HashSet<string> BlacklistedCommands { get; set; } = new();

    /// <summary>
    /// Commands requiring elevated privileges
    /// </summary>
    public Dictionary<string, PrivilegeLevel> CommandPrivilegeRequirements { get; set; } = new();

    /// <summary>
    /// Enable dangerous command detection
    /// </summary>
    public bool EnableDangerousCommandDetection { get; set; } = true;

    /// <summary>
    /// Allowed command categories
    /// </summary>
    public HashSet<string> AllowedCategories { get; set; } = new();
}

public enum ValidationMode
{
    Whitelist,    // Only allow listed commands
    Blacklist,    // Block listed commands
    Disabled      // No validation
}

public enum PrivilegeLevel
{
    Standard,     // Normal user
    Elevated,     // sudo/admin
    Restricted    // Limited commands
}
```

### CommandValidationService

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Service for validating commands before execution
/// </summary>
public class CommandValidationService
{
    private readonly SecurityPolicy _policy;
    private readonly List<ICommandValidator> _validators;
    private readonly ILogger<CommandValidationService>? _logger;

    public CommandValidationService(
        SecurityPolicy policy,
        ILogger<CommandValidationService>? logger = null)
    {
        _policy = policy;
        _logger = logger;
        _validators = new List<ICommandValidator>
        {
            new WhitelistValidator(policy),
            new BlacklistValidator(policy),
            new PrivilegeValidator(policy),
            new DangerousCommandValidator(policy),
            new CategoryValidator(policy)
        };
    }

    /// <summary>
    /// Validates a command
    /// </summary>
    public CommandValidationResult Validate(
        string command,
        CommandContext context,
        PrivilegeLevel? currentPrivilegeLevel = null)
    {
        var privilege = currentPrivilegeLevel ?? _policy.DefaultPrivilegeLevel;

        foreach (var validator in _validators)
        {
            var result = validator.Validate(command, context, privilege);
            if (!result.IsValid)
            {
                _logger?.LogWarning("Command {Command} rejected: {Reason}",
                    command, result.FailureReason);
                return result;
            }

            if (result.Status == CommandValidationStatus.AllowedWithWarning)
            {
                _logger?.LogWarning("Command {Command} has warning: {Reason}",
                    command, result.FailureReason);
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
        var commandName = ExtractCommandName(command);
        return _policy.CommandPrivilegeRequirements.TryGetValue(
            commandName, out var level) && level > PrivilegeLevel.Standard;
    }

    private static string ExtractCommandName(string command)
    {
        var parts = command.TrimStart('/').Split(' ');
        return parts[0].ToLowerInvariant();
    }
}
```

---

## Built-in Validators

### WhitelistValidator

```csharp
public class WhitelistValidator : ICommandValidator
{
    private readonly SecurityPolicy _policy;

    public string Name => "WhitelistValidator";

    public CommandValidationResult Validate(
        string command,
        CommandContext context,
        PrivilegeLevel privilege)
    {
        if (_policy.Mode != ValidationMode.Whitelist)
            return CommandValidationResult.Success();

        var commandName = ExtractCommandName(command);
        if (_policy.WhitelistedCommands.Contains(commandName))
            return CommandValidationResult.Success();

        return CommandValidationResult.Forbidden(
            $"Command '/{commandName}' is not in the whitelist");
    }
}
```

### BlacklistValidator

```csharp
public class BlacklistValidator : ICommandValidator
{
    private readonly SecurityPolicy _policy;

    public string Name => "BlacklistValidator";

    public CommandValidationResult Validate(
        string command,
        CommandContext context,
        PrivilegeLevel privilege)
    {
        var commandName = ExtractCommandName(command);
        if (_policy.BlacklistedCommands.Contains(commandName))
            return CommandValidationResult.Forbidden(
                $"Command '/{commandName}' is blacklisted");

        return CommandValidationResult.Success();
    }
}
```

### DangerousCommandValidator

```csharp
public class DangerousCommandValidator : ICommandValidator
{
    private static readonly Regex[] DangerousPatterns =
    {
        new Regex(@"[;&|`$]\s*\w", RegexOptions.Compiled),  // Command chaining
        new Regex(@"\.\.\/", RegexOptions.Compiled),         // Path traversal
        new Regex(@"\|\s*\(|\)\s*\|", RegexOptions.Compiled), // Pipeline injection
        new Regex(@"\$\{?\w+\}?", RegexOptions.Compiled),    // Variable expansion
    };

    public string Name => "DangerousCommandValidator";

    public CommandValidationResult Validate(
        string command,
        CommandContext context,
        PrivilegeLevel privilege)
    {
        if (!_policy.EnableDangerousCommandDetection)
            return CommandValidationResult.Success();

        foreach (var pattern in DangerousPatterns)
        {
            if (pattern.IsMatch(command))
            {
                return CommandValidationResult.Forbidden(
                    "Command contains potentially dangerous patterns");
            }
        }

        return CommandValidationResult.Success();
    }
}
```

---

## Integration Points

### With CommandDispatcher (Task 6.1)
```csharp
// Before executing command
var result = validationService.Validate(command, context, privilege);
if (!result.IsValid)
{
    return CommandResult.Error("Validation failed: " + result.FailureReason);
}
```

### With PrivilegeTracker (Task 4.4)
```csharp
// Get current privilege level
var privilege = privilegeTracker.CurrentLevel;
var validation = validationService.Validate(command, context, privilege);
```

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Validation disabled | Allow all commands |
| Whitelist empty | Allow nothing in whitelist mode |
| Invalid policy | Use defaults, log warning |
| Validator error | Log error, allow command |

---

## Testing

### Unit Tests

```csharp
[Fact]
public void Validate_BlacklistedCommand_ReturnsForbidden()
{
    // Arrange
    var policy = new SecurityPolicy
    {
        Mode = ValidationMode.Blacklist,
        BlacklistedCommands = { "rm", "format" }
    };
    var service = new CommandValidationService(policy);
    var context = new CommandContext();

    // Act
    var result = service.Validate("/rm -rf /", context);

    // Assert
    Assert.False(result.IsValid);
    Assert.Equal(CommandValidationStatus.Forbidden, result.Status);
}

[Fact]
public void Validate_WhitelistMode_OnlyAllowsWhitelisted()
{
    // Arrange
    var policy = new SecurityPolicy
    {
        Mode = ValidationMode.Whitelist,
        WhitelistedCommands = { "help", "status" }
    };
    var service = new CommandValidationService(policy);
    var context = new CommandContext();

    // Act
    var result1 = service.Validate("/help", context);
    var result2 = service.Validate("/clear", context);

    // Assert
    Assert.True(result1.IsValid);
    Assert.False(result2.IsValid);
}

[Fact]
public void Validate_DangerousPattern_ReturnsForbidden()
{
    // Arrange
    var policy = new SecurityPolicy
    {
        EnableDangerousCommandDetection = true
    };
    var service = new CommandValidationService(policy);
    var context = new CommandContext();

    // Act
    var result = service.Validate("/clear; rm -rf /", context);

    // Assert
    Assert.False(result.IsValid);
}
```

---

## Acceptance Criteria

- [ ] Whitelist mode works
- [ ] Blacklist mode works
- [ ] Dangerous pattern detection works
- [ ] Privilege levels are enforced
- [ ] Category restrictions work
- [ ] Validation in < 1ms
- [ ] Custom validators supported
- [ ] Policy configuration works
- [ ] Integration with CommandDispatcher

---

## Files Created

```
src/Security/
├── ICommandValidator.cs        # Validator interface
├── CommandValidationResult.cs  # Result types
├── SecurityPolicy.cs           # Policy definitions
├── CommandValidationService.cs # Main service
├── Validators/
│   ├── WhitelistValidator.cs
│   ├── BlacklistValidator.cs
│   ├── PrivilegeValidator.cs
│   ├── DangerousCommandValidator.cs
│   └── CategoryValidator.cs
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| ICommandValidator.cs | Low | ~30 |
| CommandValidationResult.cs | Low | ~60 |
| SecurityPolicy.cs | Low | ~80 |
| CommandValidationService.cs | Medium | ~120 |
| Validators (5 files) | Low | ~150 |

**Total Estimated:** ~440 lines of C#

---

## Next Steps

After Task 7.2 is complete:
1. Task 7.3: Audit Logging
2. Phase 7 Complete Summary

---

## Notes

- Default policy uses blacklist mode
- Dangerous command detection is on by default
- All validators are composable
- Consider adding rate limiting
- Logging of validation failures
