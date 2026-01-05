# Task 6.2 Specification: Core Commands Implementation

## Task: Implement Core Command Handlers

**Phase:** Phase 6: Slash Commands  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.1 Complete

---

## Description

Implement the three core slash commands that provide essential functionality:
- `/help` - Display help information
- `/clear` - Clear chat history
- `/context` - Manage context window

These commands are fundamental to user interaction and will be the most frequently used commands.

---

## Deliverables

### 1. HelpCommandHandler.cs
Display help information:
- List all available commands
- Show detailed help for specific command
- Suggest similar commands for typos

### 2. ClearCommandHandler.cs
Clear chat history:
- Clear all (messages + context)
- Clear messages only
- Clear context only

### 3. ContextCommandHandler.cs
Manage context window:
- Show current context settings
- Set fixed line count
- Set percentage of max
- Set auto mode

---

## Requirements

### Functional Requirements

#### /help Command

| Requirement | Description |
|-------------|-------------|
| Command List | Display all available commands organized by category |
| Detailed Help | Show full syntax and examples for specific command |
| Command Search | Find commands by name or description |
| Suggestions | Suggest similar commands for unrecognized names |
| Alias Support | Recognize `/h` and `/?` as `/help` |

#### /clear Command

| Requirement | Description |
|-------------|-------------|
| Clear All | Clear messages and context |
| Clear Messages | Clear chat history only |
| Clear Context | Clear context window only |
| Confirmation | Warn before clearing (if implemented) |
| Count Display | Show how many items were cleared |

#### /context Command

| Requirement | Description |
|-------------|-------------|
| Show Settings | Display current context configuration |
| Set Lines | Set exact line count (e.g., `/context 100`) |
| Set Percentage | Set percentage of max (e.g., `/context 75%`) |
| Set Auto | Enable auto-sizing based on model |
| Validation | Reject invalid values (negative, >100%) |

### Command Syntax

```
/help
/help <command>

/clear
/clear all
/clear messages
/clear context

/context
/context <lines>
/context <percentage>%
/context --percentage <0-100>
/context --auto
```

### Examples

```
/help
/help context
/help model

/clear
/clear messages
/clear all

/context
/context 100
/context 50%
/context --percentage 75
/context --auto
```

---

## Implementation

### HelpCommandHandler

```csharp
public class HelpCommandHandler : CommandHandlerBase
{
    private readonly CommandRegistry _registry;

    public HelpCommandHandler(CommandRegistry registry)
    {
        _registry = registry;
    }

    public override CommandMetadata Metadata => new()
    {
        Name = "help",
        Description = "Displays help information for commands",
        Syntax = "/help [command]",
        Examples = ["/help", "/help context", "/help model"],
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
}
```

### ClearCommandHandler

```csharp
public class ClearCommandHandler : CommandHandlerBase
{
    public override CommandMetadata Metadata => new()
    {
        Name = "clear",
        Description = "Clears chat history and resets the conversation",
        Syntax = "/clear [all|context|messages]",
        Examples = ["/clear", "/clear messages", "/clear all"],
        Category = "Core",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["cls", "reset"]
    };

    public override bool CanExecute(CommandContext context)
    {
        return context.ChatPane != null || context.Messages.Count > 0;
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var scope = command.Arguments.Count > 0 
            ? command.Arguments[0].ToLowerInvariant() 
            : "all";

        return scope switch
        {
            "all" => ClearAll(context),
            "context" => ClearContext(context),
            "messages" => ClearMessages(context),
            _ => InvalidArguments("/clear [all|context|messages]")
        };
    }
}
```

### ContextCommandHandler

```csharp
public class ContextCommandHandler : CommandHandlerBase
{
    public override CommandMetadata Metadata => new()
    {
        Name = "context",
        Description = "Manages the context window size",
        Syntax = "/context [show|<lines>|--percentage <0-100>|--auto]",
        Examples = ["/context", "/context 100", "/context 50%", "/context --auto"],
        Category = "Core",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["ctx", "window"]
    };

    public override bool CanExecute(CommandContext context)
    {
        return context.ContextManager != null;
    }

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        if (command.Arguments.Count == 0 && command.Flags.Count == 0)
        {
            return ShowCurrentContext(context);
        }

        if (command.Flags.ContainsKey("auto"))
        {
            return SetAutoMode(context);
        }

        if (command.Flags.TryGetValue("percentage", out var pct))
        {
            return SetPercentage(context, pct);
        }

        if (command.Arguments.Count > 0)
        {
            var arg = command.Arguments[0];
            if (arg.EndsWith("%"))
            {
                return SetPercentage(context, arg.TrimEnd('%'));
            }
            if (int.TryParse(arg, out var lines))
            {
                return SetLines(context, lines);
            }
        }

        return InvalidArguments("/context [show|<lines>|--percentage <0-100>|--auto]");
    }
}
```

---

## Integration Points

### With CommandRegistry
- HelpCommandHandler receives CommandRegistry for command lookup
- Enables `/help <command>` functionality

### With CommandContext
- ClearCommandHandler uses `context.Messages`
- ContextCommandHandler uses `context.ContextManager`

### With ContextWindowManager (Task 4.1)
- Set MaxLines property
- Set Policy (Auto/Fixed/Percentage)
- Call Clear() method

---

## Error Handling

### HelpCommandHandler
- Unknown command: Show suggestions
- Empty registry: Show informative message

### ClearCommandHandler
- No messages to clear: Show "nothing to clear" message
- Clear failure: Log error, show message

### ContextCommandHandler
- No ContextManager: Return NotAvailable
- Invalid lines: Return error with valid range
- Invalid percentage: Return error with valid range

---

## Testing

### Unit Tests

```csharp
[Fact]
public void HelpCommand_ListAllCommands_ReturnsCommandList()
{
    // Arrange
    var registry = new CommandRegistry(logger);
    registry.Register(new HelpCommandHandler(registry));
    var handler = new HelpCommandHandler(registry);
    var context = new CommandContext();
    var command = new ParsedCommand { Arguments = new List<string>() };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("Available Commands", result.Response);
}

[Fact]
public void ClearCommand_ClearAll_ClearsMessagesAndContext()
{
    // Arrange
    var handler = new ClearCommandHandler();
    var context = new CommandContext
    {
        Messages = new List<ChatMessage> { new ChatMessage() },
        ContextManager = new ContextWindowManager()
    };
    var command = new ParsedCommand { Arguments = new List<string> { "all" } };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Empty(context.Messages);
}

[Fact]
public void ContextCommand_SetLines_UpdatesMaxLines()
{
    // Arrange
    var handler = new ContextCommandHandler();
    var context = new CommandContext
    {
        ContextManager = new ContextWindowManager()
    };
    var command = new ParsedCommand { Arguments = new List<string> { "100" } };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(100, context.ContextManager.MaxLines);
    Assert.Equal(ContextSizePolicy.Fixed, context.ContextManager.Policy);
}
```

---

## Acceptance Criteria

- [ ] `/help` displays organized command list
- [ ] `/help <command>` shows detailed help
- [ ] `/help` suggests similar commands for typos
- [ ] `/clear` clears all messages and context
- [ ] `/clear messages` clears only messages
- [ ] `/clear context` clears only context
- [ ] `/context` shows current settings
- [ ] `/context 100` sets 100 lines
- [ ] `/context 50%` sets 50% of max
- [ ] `/context --auto` enables auto mode
- [ ] Invalid arguments show correct syntax
- [ ] All commands have aliases
- [ ] All commands are properly categorized

---

## Files Created

```
src/Commands/Handlers/
├── HelpCommandHandler.cs
├── ClearCommandHandler.cs
└── ContextCommandHandler.cs
```

---

## Estimated Complexity

| Handler | Complexity | Lines |
|---------|------------|-------|
| HelpCommandHandler | Low | ~80 |
| ClearCommandHandler | Low | ~100 |
| ContextCommandHandler | Medium | ~130 |

**Total Estimated:** ~310 lines of C#

---

## Next Steps

After Task 6.2 is complete:
1. Task 6.3: Implement Configuration Commands (/model, /mode, /theme)
2. Task 6.4: Implement Utility Commands (/save-log, /export, /filter)

---

## Notes

- All commands use CommandHandlerBase for common functionality
- Markdown formatting for help text output
- Comprehensive error handling with user-friendly messages
- Logger injection for observability
- Thread-safe implementations
