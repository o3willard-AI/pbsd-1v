# Task 8.2 Specification: Command Reference Documentation

## Task: Implement Complete Command Reference

**Phase:** Phase 8: Help & Documentation  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 8.1 complete

---

## Description

Create a comprehensive command reference documentation system that provides detailed, searchable documentation for all PairAdmin slash commands.

---

## Deliverables

### 1. CommandReference.cs
Command reference data:
- Complete command metadata
- Examples and use cases
- Related commands
- Category organization

### 2. CommandReferenceService.cs
Reference service:
- Search functionality
- Category browsing
- Command comparison
- Export capabilities

### 3. CommandReferenceContent.cs
Detailed documentation:
- All 15 commands documented
- Examples for each command
- Common use cases
- Error handling

---

## Requirements

### Command Documentation Format

Each command should have:

```yaml
Command: help
Aliases: h, ?
Category: Core
Description: Displays help information
Syntax: "/help [command]"
Examples:
  - "/help"
  - "/help context"
Arguments:
  - name: command
    required: false
    description: Specific command to get help for
    type: string
Related Commands:
  - context
  - status
Notes: |
  Additional notes about the command
```

### Categories

1. **Core Commands** - Essential daily commands
2. **Configuration** - Settings management
3. **Utility** - Helpful utilities
4. **Security** - Security features

### Search Features

- Full-text search
- Command name search
- Category filter
- Tag-based search

---

## Implementation

### CommandReference Model

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Complete command reference documentation
/// </summary>
public class CommandReference
{
    /// <summary>
    /// Command name (without /)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full command with /
    /// </summary>
    public string Command => "/" + Name;

    /// <summary>
    /// Short description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string DetailedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Syntax example
    /// </summary>
    public string Syntax { get; set; } = string.Empty;

    /// <summary>
    /// Usage examples
    /// </summary>
    public List<string> Examples { get; set; } = new();

    /// <summary>
    /// Command arguments
    /// </summary>
    public List<CommandArgument> Arguments { get; set; } = new();

    /// <summary>
    /// Named flags
    /// </summary>
    public List<CommandFlag> Flags { get; set; } = new();

    /// <summary>
    /// Aliases
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// Related commands
    /// </summary>
    public List<string> RelatedCommands { get; set; } = new();

    /// <summary>
    /// Tips and notes
    /// </summary>
    public List<string> Tips { get; set; } = new();

    /// <summary>
    /// Error handling
    /// </summary>
    public List<CommandError> CommonErrors { get; set; } = new();

    /// <summary>
    /// Minimum privilege required
    /// </summary>
    public PrivilegeLevel RequiredPrivilege { get; set; } = PrivilegeLevel.Standard;
}

/// <summary>
/// Command argument definition
/// </summary>
public class CommandArgument
{
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public List<string>? ValidValues { get; set; }
}

/// <summary>
/// Command flag definition
/// </summary>
public class CommandFlag
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortForm { get; set; }
    public bool RequiresValue { get; set; }
}

/// <summary>
/// Common error handling
/// </summary>
public class CommandError
{
    public string Condition { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
}
```

### CommandReferenceService

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Service for command reference documentation
/// </summary>
public class CommandReferenceService
{
    private readonly Dictionary<string, CommandReference> _commands;

    public CommandReferenceService()
    {
        _commands = new Dictionary<string, CommandReference>(StringComparer.OrdinalIgnoreCase);
        InitializeReferences();
    }

    /// <summary>
    /// Gets reference for a command
    /// </summary>
    public CommandReference? GetCommand(string name)
    {
        var normalized = name.TrimStart('/').ToLowerInvariant();
        return _commands.TryGetValue(normalized, out var cmd) ? cmd : null;
    }

    /// <summary>
    /// Gets all commands in a category
    /// </summary>
    public IEnumerable<CommandReference> GetByCategory(string category)
    {
        return _commands.Values
            .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name);
    }

    /// <summary>
    /// Searches commands
    /// </summary>
    public IEnumerable<CommandReference> Search(string query)
    {
        var q = query.ToLowerInvariant();
        return _commands.Values
            .Where(c =>
                c.Name.Contains(q) ||
                c.Description.Contains(q) ||
                c.Category.Contains(q) ||
                c.Aliases.Any(a => a.Contains(q)))
            .OrderBy(c => c.Name);
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        return _commands.Values
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <summary>
    /// Gets command summary table
    /// </summary>
    public string GetSummaryTable()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Command Reference");
        sb.AppendLine();
        sb.AppendLine("| Command | Description | Category |");
        sb.AppendLine("|---------|-------------|----------|");

        foreach (var cmd in _commands.Values.OrderBy(c => c.Category).ThenBy(c => c.Name))
        {
            sb.AppendLine($"| `/{cmd.Name}` | {cmd.Description} | {cmd.Category} |");
        }

        return sb.ToString();
    }

    private void InitializeReferences()
    {
        InitializeCoreCommands();
        InitializeConfigCommands();
        InitializeUtilityCommands();
        InitializeSecurityCommands();
    }

    private void InitializeCoreCommands()
    {
        _commands["help"] = new CommandReference
        {
            Name = "help",
            Description = "Displays help information for commands",
            DetailedDescription = "The help command provides access to PairAdmin's comprehensive documentation. " +
                "You can view all commands, get detailed help for specific commands, or search for topics.",
            Category = "Core",
            Syntax = "/help [command|category|search <query>]",
            Examples = new List<string>
            {
                "/help",
                "/help context",
                "/help core-commands",
                "/help search model"
            },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument
                {
                    Name = "command|category|search",
                    Required = false,
                    Description = "Specific command, category name, or 'search' keyword"
                }
            },
            Aliases = new List<string> { "h", "?" },
            RelatedCommands = new List<string> { "status", "context" },
            Tips = new List<string>
            {
                "Use /help search <term> to find topics",
                "All commands support /h as a shortcut"
            },
            CommonErrors = new List<CommandError>
            {
                new CommandError
                {
                    Condition = "Unknown command name",
                    Message = "Command 'xyz' not found",
                    Solution = "Use /help to see all available commands"
                }
            }
        };

        _commands["clear"] = new CommandReference
        {
            Name = "clear",
            Description = "Clears chat history and resets the conversation",
            DetailedDescription = "The clear command removes messages and/or context from the current session. " +
                "This is useful when starting a new topic or when the conversation has become too long.",
            Category = "Core",
            Syntax = "/clear [all|messages|context]",
            Examples = new List<string>
            {
                "/clear",
                "/clear messages",
                "/clear context"
            },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument
                {
                    Name = "scope",
                    Required = false,
                    Description = "Scope: 'all', 'messages', or 'context'",
                    DefaultValue = "all"
                }
            },
            Aliases = new List<string> { "cls", "reset" },
            RelatedCommands = new List<string> { "save-log", "context" },
            Tips = new List<string>
            {
                "Use /clear messages to keep context but start fresh chat",
                "Use /clear context to clear history but keep current discussion"
            }
        };

        _commands["context"] = new CommandReference
        {
            Name = "context",
            Description = "Manages the context window size",
            DetailedDescription = "The context command controls how much conversation history is sent to the AI. " +
                "A larger context provides more history but costs more tokens. Smaller context is faster but less contextual.",
            Category = "Core",
            Syntax = "/context [show|<lines>|--percentage <0-100>|--auto]",
            Examples = new List<string>
            {
                "/context",
                "/context 100",
                "/context 50%",
                "/context --percentage 75",
                "/context --auto"
            },
            Arguments = new List<CommandArgument>
            {
                new CommandArgument
                {
                    Name = "lines|percentage",
                    Required = false,
                    Description = "Number of lines or percentage with % suffix"
                }
            },
            Flags = new List<CommandFlag>
            {
                new CommandFlag
                {
                    Name = "percentage",
                    Description = "Set context as percentage of maximum",
                    RequiresValue = true
                },
                new CommandFlag
                {
                    Name = "auto",
                    Description = "Use model default context size"
                }
            },
            Aliases = new List<string> { "ctx", "window" },
            RelatedCommands = new List<string> { "clear" },
            Tips = new List<string>
            {
                "Auto mode is recommended for most users",
                "Smaller context = faster responses",
                "Larger context = more history but slower"
            }
        };
    }

    private void InitializeConfigCommands() { /* ... */ }
    private void InitializeUtilityCommands() { /* ... */ }
    private void InitializeSecurityCommands() { /* ... */ }
}
```

---

## Files Created

```
src/Help/Reference/
├── CommandReference.cs         # Reference model (150 lines)
├── CommandReferenceService.cs  # Service (200 lines)
└── CommandReferenceContent.cs  # All documentation (300 lines)
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| CommandReference.cs | Low | ~150 |
| CommandReferenceService.cs | Low | ~200 |
| CommandReferenceContent.cs | Low | ~300 |

**Total Estimated:** ~650 lines of C#

---

## Next Steps

After Task 8.2 is complete:
1. Task 8.3: Tooltip Help
2. Phase 8 Complete Summary

---

## Notes

- Include examples for every command
- Add common error solutions
- Keep documentation up to date
- Use consistent format
- Add tags for searchability
