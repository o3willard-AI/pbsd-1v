# Task 6.4 Specification: Utility Commands

## Task: Implement Utility Commands

**Phase:** Phase 6: Slash Commands  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.3 Complete

---

## Description

Implement utility commands for data management and export functionality:
- `/save-log` - Export session log
- `/export` - Export chat history
- `/filter` - Manage context filters

These commands enable users to persist and share their work.

---

## Deliverables

### 1. SaveLogCommandHandler.cs
Export session log to file:
- Export to text format
- Export to JSON format
- Custom file path support

### 2. ExportCommandHandler.cs
Export chat history:
- Export all messages
- Export with metadata
- Copy to clipboard

### 3. FilterCommandHandler.cs
Manage context filters:
- Add filter patterns
- Remove filter patterns
- List active filters
- Clear all filters

---

## Requirements

### Functional Requirements

#### /save-log Command

| Requirement | Description |
|-------------|-------------|
| Default Export | Export to default location |
| Custom Path | Specify output file path |
| Format Selection | Text or JSON format |
| Overwrite Prompt | Warn if file exists |
| Success Notification | Show export location |

#### /export Command

| Requirement | Description |
|-------------|-------------|
| Full Export | Export entire chat history |
| Format Options | Text or JSON format |
| Copy to Clipboard | Quick copy functionality |
| Include Metadata | Include timestamps, roles |
| Exclude System | Option to exclude system messages |

#### /filter Command

| Requirement | Description |
|-------------|-------------|
| Add Pattern | Add new filter pattern |
| Remove Pattern | Remove existing filter |
| List Filters | Show all active filters |
| Clear All | Remove all filters |
| Pattern Types | Text match, regex support |

### Command Syntax

```
/save-log
/save-log <path>
/ave-log --format json <path>

/export
/export --format text
/export --format json
/export --copy

/filter
/filter add <pattern>
/filter remove <pattern>
/filter list
/filter clear
/filter --regex <pattern>
```

### Examples

```
/save-log
/save-log /home/user/session.log
/save-log --format json session.json

/export
/export --format json
/export --copy

/filter
/filter add "password:"
/filter remove "password:"
/filter list
/filter clear
/filter --regex "(?i)secret"
```

---

## Implementation

### SaveLogCommandHandler

```csharp
public class SaveLogCommandHandler : CommandHandlerBase
{
    private readonly string _defaultLogPath;

    public override CommandMetadata Metadata => new()
    {
        Name = "save-log",
        Description = "Exports the session log to a file",
        Syntax = "/save-log [path] [--format text|json]",
        Examples = ["/save-log", "/save-log session.log", "/save-log --format json out.json"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 1,
        Aliases = ["log", "save"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var format = command.Flags.GetValueOrDefault("format", "text");
        var path = command.Arguments.Count > 0 ? command.Arguments[0] : GetDefaultPath();

        return ExportLog(context, path, format);
    }
}
```

### ExportCommandHandler

```csharp
public class ExportCommandHandler : CommandHandlerBase
{
    public override CommandMetadata Metadata => new()
    {
        Name = "export",
        Description = "Exports chat history to clipboard or file",
        Syntax = "/export [--format text|json] [--copy]",
        Examples = ["/export", "/export --format json", "/export --copy"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 0,
        Aliases = ["save-chat", "download"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var copy = command.Flags.ContainsKey("copy");
        var format = command.Flags.GetValueOrDefault("format", "text");

        if (copy)
        {
            return CopyToClipboard(context, format);
        }

        return ExportChat(context, format);
    }
}
```

### FilterCommandHandler

```csharp
public class FilterCommandHandler : CommandHandlerBase
{
    private readonly List<string> _filters;

    public override CommandMetadata Metadata => new()
    {
        Name = "filter",
        Description = "Manages context filters for sensitive data",
        Syntax = "/filter [add|remove|list|clear] <pattern> [--regex]",
        Examples = ["/filter list", "/filter add password:", "/filter --regex secret"],
        Category = "Utility",
        IsAvailable = true,
        MinArguments = 0,
        MaxArguments = 2,
        Aliases = ["mask", "redact"]
    };

    public override CommandResult Execute(CommandContext context, ParsedCommand command)
    {
        var subCommand = command.Arguments.Count > 0 ? command.Arguments[0].ToLowerInvariant() : "list";
        var pattern = command.Arguments.Count > 1 ? command.Arguments[1] : null;
        var isRegex = command.Flags.ContainsKey("regex");

        return subCommand switch
        {
            "add" => AddFilter(pattern, isRegex),
            "remove" => RemoveFilter(pattern),
            "list" => ListFilters(),
            "clear" => ClearFilters(),
            _ => InvalidArguments("/filter [add|remove|list|clear] <pattern>")
        };
    }
}
```

---

## Integration Points

### With ChatPane (Task 3.1)
- Access message history via `context.Messages`
- Include message content and metadata

### With ContextWindowManager (Task 4.1)
- Filter context before sending to LLM
- Apply filters to terminal output

### With File System
- Save logs to specified paths
- Create directories if needed

---

## Error Handling

### SaveLogCommandHandler
| Scenario | Handling |
|----------|----------|
| Invalid path | Error with path format |
| Directory doesn't exist | Create or error |
| File exists | Warn or overwrite |
| Write error | Exception with details |

### ExportCommandHandler
| Scenario | Handling |
|----------|----------|
| No messages | Inform user |
| Copy failed | Error notification |
| Invalid format | Use default |

### FilterCommandHandler
| Scenario | Handling |
|----------|----------|
| Empty pattern | Error |
| Pattern not found | Inform user |
| Invalid regex | Error with details |
| No filters | Inform user |

---

## Testing

### Unit Tests

```csharp
[Fact]
public void SaveLogCommand_DefaultPath_CreatesLogFile()
{
    // Arrange
    var handler = new SaveLogCommandHandler();
    var context = new CommandContext { Messages = new List<ChatMessage>() };
    var command = new ParsedCommand { Arguments = new List<string>() };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("saved", result.Response);
}

[Fact]
public void ExportCommand_CopyToClipboard_CopiesContent()
{
    // Arrange
    var handler = new ExportCommandHandler();
    var context = new CommandContext
    {
        Messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Hello" }
        }
    };
    var command = new ParsedCommand { Flags = new Dictionary<string, string> { ["copy"] = "true" } };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("clipboard", result.Response);
}

[Fact]
public void FilterCommand_AddFilter_AddsToList()
{
    // Arrange
    var handler = new FilterCommandHandler();
    var context = new CommandContext();
    var command = new ParsedCommand { Arguments = new List<string> { "add", "password:" } };

    // Act
    var result = handler.Execute(context, command);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Contains("password:", result.Response);
}
```

---

## Acceptance Criteria

- [ ] `/save-log` exports to default location
- [ ] `/save-log <path>` exports to specified path
- [ ] `/save-log --format json` exports JSON format
- [ ] `/export` exports chat history
- [ ] `/export --copy` copies to clipboard
- [ ] `/export --format json` exports JSON
- [ ] `/filter add <pattern>` adds filter
- [ ] `/filter remove <pattern>` removes filter
- [ ] `/filter list` shows active filters
- [ ] `/filter clear` removes all filters
- [ ] `/filter --regex <pattern>` uses regex
- [ ] Invalid patterns show helpful errors

---

## Files Created

```
src/Commands/Handlers/
├── SaveLogCommandHandler.cs
├── ExportCommandHandler.cs
└── FilterCommandHandler.cs
```

---

## Estimated Complexity

| Handler | Complexity | Lines |
|---------|------------|-------|
| SaveLogCommandHandler | Medium | ~120 |
| ExportCommandHandler | Medium | ~100 |
| FilterCommandHandler | Medium | ~130 |

**Total Estimated:** ~350 lines of C#

---

## Next Steps

After Task 6.4 is complete:
- Task 6.5: Additional Commands (/status, /config, /quit)
- Task 6.6-6.10: Remaining commands
- Phase 7: Security Implementation

---

## Notes

- Use `System.Text.Json` for JSON export
- Implement proper file handling with dispose
- Add clipboard integration for WPF
- Consider async file operations
- Add progress indication for large exports
