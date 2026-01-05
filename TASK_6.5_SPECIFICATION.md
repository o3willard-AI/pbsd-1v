# Task 6.5 Specification: Additional Commands

## Task: Implement Additional Commands

**Phase:** Phase 6: Slash Commands  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.4 Complete

---

## Description

Implement additional utility commands for system management:
- `/status` - Show system status
- `/config` - Open configuration file
- `/quit` - Close application

These commands provide essential system management functionality.

---

## Deliverables

### 1. StatusCommandHandler.cs
Show system status:
- Memory usage
- Message count
- Provider status
- Context window status

### 2. ConfigCommandHandler.cs
Manage configuration:
- Show configuration path
- Open configuration file
- Reload configuration

### 3. QuitCommandHandler.cs
Close the application:
- Confirm before closing
- Save unsaved data
- Clean shutdown

---

## Requirements

### Functional Requirements

#### /status Command

| Requirement | Description |
|-------------|-------------|
| Memory Usage | Show current memory consumption |
| Message Count | Display number of messages |
| Provider Status | Show active LLM provider |
| Context Status | Display context window info |
| Uptime | Show application runtime |

#### /config Command

| Requirement | Description |
|-------------|-------------|
| Show Path | Display configuration file path |
| Open File | Open in default editor |
| Reload | Reload configuration from disk |
| Edit | Quick edit inline (optional) |

#### /quit Command

| Requirement | Description |
|-------------|-------------|
| Confirm | Ask for confirmation |
| Save Data | Save unsaved settings |
| Shutdown | Close application gracefully |
| Cancel | Abort if user declines |

### Command Syntax

```
/status

/config
/config show
/config open
/config reload

/quit
/quit --force
/quit --save
```

### Examples

```
/status

/config
/config show
/config open

/quit
/quit --save
```

---

## Implementation

### StatusCommandHandler

```csharp
public class StatusCommandHandler : CommandHandlerBase
{
    private readonly LLMGateway.LLMGateway _gateway;
    private readonly DateTime _startTime;

    public StatusCommandHandler(
        LLMGateway.LLMGateway gateway,
        ILogger<StatusCommandHandler>? logger = null)
        : base(logger)
    {
        _gateway = gateway;
        _startTime = DateTime.Now;
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "status",
        Description = "Shows system status and diagnostics",
        Syntax = "/status",
        Examples = ["/status"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["stats", "info"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        return ShowStatus(context);
    }
}
```

### ConfigCommandHandler

```csharp
public class ConfigCommandHandler : CommandHandlerBase
{
    private readonly Configuration.SettingsProvider _settingsProvider;

    public ConfigCommandHandler(
        Configuration.SettingsProvider settingsProvider,
        ILogger<ConfigCommandHandler>? logger = null)
        : base(logger)
    {
        _settingsProvider = settingsProvider;
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "config",
        Description = "Manages application configuration",
        Syntax = "/config [show|open|reload]",
        Examples = ["/config", "/config show", "/config open"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["settings", "conf"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var action = command.Arguments.Count > 0 ? command.Arguments[0].ToLowerInvariant() : "show";

        return action switch
        {
            "show" => ShowConfigPath(),
            "open" => OpenConfigFile(),
            "reload" => ReloadConfig(),
            _ => InvalidArguments("/config [show|open|reload]")
        };
    }
}
```

### QuitCommandHandler

```csharp
public class QuitCommandHandler : CommandHandlerBase
{
    public override CommandMetadata Metadata => new()
    {
        Name = "quit",
        Description = "Closes the application",
        Syntax = "/quit [--force]",
        Examples = ["/quit", "/quit --force"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["exit", "close", "q"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var force = command.Flags.ContainsKey("force");

        if (!force)
        {
            return RequestConfirmation();
        }

        return Shutdown();
    }
}
```

---

## Integration Points

### With LLMGateway (Task 3.2)
- Get active provider status
- Show provider capabilities

### With SettingsProvider (Task 4.4)
- Get configuration path
- Reload settings

### With Application (MainWindow)
- Request shutdown
- Handle confirmation

---

## Error Handling

### StatusCommandHandler
| Scenario | Handling |
|----------|----------|
| No active provider | Show "not configured" |
| Gateway unavailable | Show error |

### ConfigCommandHandler
| Scenario | Handling |
|----------|----------|
| File not found | Show default path |
| Open failed | Show path to user |
| Reload failed | Show error |

### QuitCommandHandler
| Scenario | Handling |
|----------|----------|
| User declines | Return cancelled |
| Shutdown fails | Show error |

---

## Testing

### Unit Tests

```csharp
[Fact]
public void StatusCommand_ShowStatus_ReturnsStatusInfo()
{
    // Arrange
    var gateway = new LLMGateway(logger);
    var handler = new StatusCommandHandler(gateway);
    var context = new CommandContext();
    var command = new ParsedCommand();

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("Messages", result.Response);
    Assert.Contains("Provider", result.Response);
}

[Fact]
public void ConfigCommand_ShowPath_ReturnsPath()
{
    // Arrange
    var provider = new SettingsProvider(logger);
    var handler = new ConfigCommandHandler(provider);
    var context = new CommandContext();
    var command = new ParsedCommand { Arguments = new List<string> { "show" } };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("settings.json", result.Response);
}
```

---

## Acceptance Criteria

- [ ] `/status` shows memory usage
- [ ] `/status` shows message count
- [ ] `/status` shows provider status
- [ ] `/status` shows context info
- [ ] `/config` shows configuration path
- [ ] `/config open` opens editor
- [ ] `/config reload` reloads settings
- [ ] `/quit` requests confirmation
- [ ] `/quit --force` closes without prompt
- [ ] All commands have aliases

---

## Files Created

```
src/Commands/Handlers/
├── StatusCommandHandler.cs
├── ConfigCommandHandler.cs
└── QuitCommandHandler.cs
```

---

## Estimated Complexity

| Handler | Complexity | Lines |
|---------|------------|-------|
| StatusCommandHandler | Low | ~100 |
| ConfigCommandHandler | Low | ~100 |
| QuitCommandHandler | Low | ~80 |

**Total Estimated:** ~280 lines of C#

---

## Remaining Phase 6 Tasks

After Task 6.5:
- Task 6.6-6.10: Additional commands as needed
- All tasks complete Phase 6

---

## Notes

- StatusCommandHandler shows uptime since session start
- ConfigCommandHandler uses SettingsProvider.GetSettingsPath()
- QuitCommandHandler requires MainWindow integration for actual shutdown
- Consider adding more diagnostic info to /status
