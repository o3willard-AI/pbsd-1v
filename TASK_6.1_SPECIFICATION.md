# Task 6.1 Specification: Slash Command Parser and Dispatcher

## Task: Implement Slash Command Parser and Dispatcher

**Phase:** Phase 6: Slash Commands
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Create a parser that recognizes slash commands from chat input and routes them to appropriate handlers. This enables users to control the application through chat commands.

---

## Deliverables

### 1. SlashCommandParser.cs
Slash command parser:
- Command pattern recognition
- Argument extraction
- Command syntax validation
- Help text generation

### 2. CommandDispatcher.cs
Command dispatcher:
- Routes commands to handlers
- Handles unknown commands
- Command execution with error handling
- Response generation

### 3. ICommandHandler.cs
Command handler interface:
- Common handler interface
- Execute method
- Help text property
- Command metadata

### 4. CommandRegistry.cs
Command registry:
- Command registration system
- Command lookup
- Duplicate prevention
- Category organization

---

## Requirements

### Functional Requirements

1. **Command Parsing**
   - Recognize commands starting with `/`
   - Extract command name and arguments
   - Support quoted arguments
   - Handle escaped characters

2. **Command Routing**
   - Route to appropriate handler
   - Handle unknown commands gracefully
   - Provide helpful error messages
   - Log command execution

3. **Command Registration**
   - Register commands at startup
   - Dynamic command registration
   - Category-based organization
   - Command metadata

4. **Handler Interface**
   - Common interface for all handlers
   - Execute method with context
   - Help text generation
   - Command metadata

### Non-Functional Requirements

1. **Performance**
   - Fast command parsing
   - Minimal overhead
   - Efficient handler lookup

2. **Extensibility**
   - Easy to add new commands
   - Plugin architecture
   - Custom categories

3. **User Experience**
   - Clear error messages
   - Helpful command suggestions
   - Consistent formatting

---

## Data Models

### CommandResult
```csharp
public class CommandResult
{
    public bool IsSuccess { get; set; }
    public string Response { get; set; }
    public string? ErrorMessage { get; set; }
    public CommandStatus Status { get; set; }
}

public enum CommandStatus
{
    Executed,
    NotFound,
    InvalidArguments,
    Error,
    Cancelled
}
```

### ParsedCommand
```csharp
public class ParsedCommand
{
    public string OriginalText { get; set; }
    public string CommandName { get; set; }
    public List<string> Arguments { get; set; }
    public Dictionary<string, string> Flags { get; set; }
    public string? RawArguments { get; set; }
}
```

### CommandMetadata
```csharp
public class CommandMetadata
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Syntax { get; set; }
    public List<string> Examples { get; set; }
    public string Category { get; set; }
    public bool IsAvailable { get; set; }
}
```

---

## Architecture

```
Slash Command System

User Input (/help context)
    ↓
    SlashCommandParser
    ├── ParseCommand() - Extract name and args
    ├── ValidateSyntax() - Check format
    └── GetSuggestions() - Autocomplete
    ↓
    ParsedCommand
    ↓
    CommandDispatcher
    ├── FindHandler() - Look up command
    ├── Execute() - Run handler
    └── FormatResponse() - Generate output
    ↓
    CommandResult
    ↓
    ChatPane (display response)
```

### Handler Flow

```
ParsedCommand
    ↓
    CommandRegistry.FindHandler("help")
    ↓
    ICommandHandler
    ├── CanExecute(context) - Check permissions
    └── Execute(context, args)
    ↓
    CommandResult
    ↓
    Response
```

---

## Supported Commands

### Core Commands (Task 6.2)
- `/help` - Display help
- `/clear` - Clear chat history
- `/context` - Manage context window

### Configuration Commands (Task 6.3)
- `/model` - Change LLM model
- `/mode` - Change autonomy mode
- `/theme` - Change UI theme

### Utility Commands (Task 6.4)
- `/save-log` - Export session log
- `/export` - Export chat
- `/filter` - Manage filters

### Future Commands
- `/reset` - Reset session
- `/status` - Show system status
- `/config` - Open configuration
- `/quit` - Exit application

---

## Command Syntax

### Basic Syntax
```
/command
/command arg1 arg2 arg3
/command "argument with spaces"
/command --flag value
```

### Examples
```
/help
/help context
/context 50
/context --percentage 75
/model gpt-4
/theme dark
```

### Argument Parsing
- Space-separated arguments
- Quoted strings for multi-word arguments
- `--flag value` for named parameters
- Escape sequences for special characters

---

## Integration Points

### Dependencies
- Task 3.1: ChatPane (input/output)
- Task 4.1: ContextWindowManager (/context command)
- Task 3.2: LLMGateway (/model command)
- Task 5.3: AutonomyManager (/mode command)

### Integration with ChatPane
```csharp
// Subscribe to slash commands
commandDispatcher.CommandExecuted += (s, e) =>
{
    chatPane.AddSystemMessage(e.Response);
};

// Detect slash commands in input
chatPane.MessageSent += (s, e) =>
{
    if (e.Message.Content.StartsWith("/"))
    {
        var result = commandDispatcher.Execute(e.Message.Content);
        e.CancelSend = true; // Don't send to LLM
        chatPane.AddSystemMessage(result.Response);
    }
};
```

---

## Technical Considerations

### Parsing Algorithm
1. Check if input starts with `/`
2. Extract command name (first word)
3. Parse remaining text as arguments
4. Extract named flags (--flag value)
5. Validate syntax
6. Return ParsedCommand

### Handler Lookup
- Dictionary-based O(1) lookup
- Case-insensitive command names
- Alias support (e.g., `/h` for `/help`)
- Category-based organization

### Error Handling
- Unknown command: Suggest similar commands
- Invalid arguments: Show syntax help
- Execution error: Log and display message
- Permission denied: Show access message

---

## Acceptance Criteria

- [ ] Slash commands are recognized (start with /)
- [ ] Command name and arguments are extracted
- [ ] Quoted arguments are supported
- [ ] Named flags are supported
- [ ] Unknown commands show helpful error
- [ ] Commands are routed to correct handlers
- [ ] Handler interface is defined
- [ ] Command registry works
- [ ] Help text is generated
- [ ] Error messages are clear

---

## Next Steps

After Task 6.1 is complete:
1. Task 6.2: Implement Core Commands
2. Task 6.3: Implement Configuration Commands
3. Task 6.4: Implement Utility Commands

---

## Files to Create

```
/src/Commands/SlashCommandParser.cs
/src/Commands/CommandDispatcher.cs
/src/Commands/ICommandHandler.cs
/src/Commands/CommandRegistry.cs
/src/Commands/CommandResult.cs
/src/Commands/CommandMetadata.cs
/src/Commands/ParsedCommand.cs
```

---

## Dependencies

### Internal
- Task 3.1: ChatPane (events)
- Task 4.1: ContextWindowManager (context commands)
- Task 3.2: LLMGateway (model commands)

### External Libraries
- System.Text.RegularExpressions (validation)

---

## Estimated Complexity
- **SlashCommandParser.cs**: Medium (~200 lines)
- **CommandDispatcher.cs**: Medium (~180 lines)
- **ICommandHandler.cs**: Low (~50 lines)
- **CommandRegistry.cs**: Medium (~150 lines)
- **Supporting Models**: Low (~100 lines)

**Total Estimated:** ~680 lines of C# code

---

## Mockup

```
User Input: /help context

Parsed:
- CommandName: "help"
- Arguments: ["context"]

Handler: HelpCommandHandler
- Execute(helpArgs: ["context"])
- Return: Help text for /context command

Result:
"## /context command

Changes the context window size.

Usage: /context <lines>
       /context --percentage <0-100>

Examples:
  /context 100      (100 lines)
  /context 50%      (50% of max)

Current setting: 100 lines"
```

