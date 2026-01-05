# Task 4.3 Specification: Parse and Track Current Working Directory

## Task: Parse and Track Current Working Directory from Terminal

**Phase:** Phase 4: Context Awareness
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Extract and track the current working directory from terminal prompts. This allows the AI to provide context-aware suggestions and commands that reference the current directory path.

---

## Deliverables

### 1. TerminalPromptParser.cs
Terminal prompt parser:
- Detects prompt patterns
- Extracts current working directory
- Handles common shell prompts (bash, zsh, fish, PowerShell)
- Supports custom prompt patterns

### 2. WorkingDirectoryTracker.cs
Working directory tracker:
- Tracks current directory changes
- Maintains directory history
- Provides directory change events
- Integrates with context provider

### 3. DirectoryChangedEventArgs.cs
Event arguments for directory changes:
- Old directory
- New directory
- Timestamp
- Change source (user, command, prompt parse)

---

## Requirements

### Functional Requirements

1. **Prompt Parsing**
   - Detect common shell prompts
   - Extract working directory from prompt
   - Handle home directory shortcuts (~)
   - Support for multiple prompt formats

2. **Directory Tracking**
   - Track current directory state
   - Detect directory changes
   - Maintain history stack
   - Push/pop for cd commands

3. **Event System**
   - DirectoryChanged event for notifications
   - DirectoryStackChanged event
   - Context integration events

4. **Context Integration**
   - Include current directory in AI context
   - Format for LLM prompts
   - Support for privilege information

### Non-Functional Requirements

1. **Performance**
   - Fast parsing of terminal output
   - Efficient history management
   - Minimal memory overhead

2. **Reliability**
   - Accurate directory detection
   - Graceful handling of parse failures
   - Fallback when directory not detectable

3. **Extensibility**
   - Custom prompt patterns
   - Plugin architecture for parsers
   - Configurable behavior

---

## Data Models

### PromptType
```csharp
public enum PromptType
{
    Bash,          // [user@host:~]$
    Zsh,           // [user@host ~]%
    Fish,           // user@host /path/to/dir
    PowerShell,    // PS C:\Users\user>
    Custom          // User-defined pattern
}
```

### ParsedPrompt
```csharp
public class ParsedPrompt
{
    public string OriginalText { get; set; }
    public PromptType Type { get; set; }
    public string? Username { get; set; }
    public string? Hostname { get; set; }
    public string? Directory { get; set; }
    public bool IsDetected { get; set; }
}
```

### DirectoryChangeType
```csharp
public enum DirectoryChangeType
{
    UserCommand,    // User typed command
    ParsedPrompt,  // Detected from prompt
    StackOperation  // push/pop from history
}
```

### DirectoryState
```csharp
public class DirectoryState
{
    public string CurrentDirectory { get; set; }
    public Stack<string> History { get; set; }
    public int MaxHistorySize { get; set; }
}
```

---

## Shell Prompt Patterns

### Bash Prompt
```
Pattern: ^([^@]+)@([^:]+):([^ ]*)\$
Example: user@hostname:~/project$
          [user@hostname:/var/log]$
          [user@hostname:]$

Capture:
- Group 1: Username
- Group 2: Hostname
- Group 3: Current directory
```

### Zsh Prompt
```
Pattern: ^([^@]+)@([^ ]+) ([^%]+)%
Example: user@host ~/project %
          [user@host /var/log %

Capture:
- Group 1: Username
- Group 2: Hostname
- Group 3: Current directory (before %)
```

### PowerShell Prompt
```
Pattern: ^PS [^>]+> (.+)$
Example: PS C:\Users\user\Documents>
          PS C:\Program Files\app>

Capture:
- Group 1: Current directory (after >)
```

### Fish Prompt
```
Pattern: ^([^@]+)@([^ ]+) (.+)$
Example: user@host /home/user/project

Capture:
- Group 1: Username
- Group 2: Current directory (after @host)
```

---

## Architecture

```
Working Directory Tracking System

Terminal Output (via IO Interceptor)
    ↓
    TerminalPromptParser
        ↓ ParsePrompt()
    ParsedPrompt
        ↓ Extract Directory
    WorkingDirectoryTracker
        ├── Directory State
        ├── History Stack
        └── Event System
            ↓ DirectoryChanged Event
    IContextProvider (Context Injection)
        ↓ AddToContext()
            Current directory in AI prompt
```

### Data Flow

```
User types: cd /home/user/project
    ↓
    Terminal outputs: user@hostname:~/project$
    ↓
    TerminalPromptParser.ParsePrompt()
    ↓ Extract: ~/project → /home/user/project
    ↓ Expand: /home/user/project (resolve ~)
    ↓
    WorkingDirectoryTracker.SetDirectory("/home/user/project")
    ↓ DirectoryChanged Event
    ↓ ContextProvider.AddToContext()
    ↓ AI Context includes: "Working directory: /home/user/project"
```

---

## Directory History

```
History Stack:

/home/user/initial           (bottom)
/home/user/project1
/home/user/project2
/home/user/project3
/current/working/dir         (top - current)

pushd/popd supported:
- pushd: Push to stack
- popd: Pop from stack
- dirs: Rotate stack
```

---

## Context Integration

### Context Format
```markdown
Terminal context:
[previous terminal output]

Working directory: /home/user/project

User question:
[question]
```

### Integration with ContextProvider
```csharp
var workingDir = workingDirectoryTracker.GetCurrentDirectory();
var context = contextProvider.GetContext();

var contextWithDir = $"Working directory: {workingDir}\n\n{context}";

contextManager.AddContext(contextWithDir);
```

---

## Integration Points

### Dependencies
- Task 2.2: IO Interceptor (terminal output)
- Task 2.4: IContextProvider (context injection)
- Task 4.1: ContextWindowManager (context management)

### Event Integration
```csharp
// Subscribe to directory changes
workingDirectoryTracker.DirectoryChanged += (s, e) =>
{
    var newContext = $"Working directory: {e.NewDirectory}";
    contextManager.UpdateContext(newContext);
};

// Inject into LLM requests
llmGateway.MessageSent += (s, e) =>
{
    var workingDir = workingDirectoryTracker.GetCurrentDirectory();
    e.Request.AddContext($"Working directory: {workingDir}");
};
```

---

## Technical Considerations

### Path Expansion
- Expand `~` to home directory
- Expand environment variables ($HOME, $USER, etc.)
- Handle relative paths
- Resolve symbolic links (optional)

### Cross-Platform
- Different prompt patterns for different shells
- Windows vs Unix path separators
- Case-insensitive path comparisons on Windows

### Error Handling
- Graceful fallback when parsing fails
- Default to user home directory
- Log parse failures for debugging
- User-configurable prompts

---

## Acceptance Criteria

- [ ] Current directory is correctly identified
- [ ] Changes are detected in real-time
- [ ] Directory is included in AI context
- [ ] Prompt parsing supports common shells
- [ ] Path expansion works (~ to home)
- [ ] History stack works (pushd/popd)
- [ ] DirectoryChanged events fire correctly
- [ ] Old and new directory values are correct
- [ ] Timestamps are captured
- [ ] Multiple prompt types supported
- [ ] Fallback works when parsing fails
- [ ] Integration with IContextProvider works

---

## Next Steps

After Task 4.3 is complete:
1. Task 4.4: Detect and Track User Privilege Levels

---

## Files to Create

```
/src/Context/TerminalPromptParser.cs
/src/Context/WorkingDirectoryTracker.cs
/src/Context/DirectoryChangedEventArgs.cs
```

---

## Dependencies

### Internal
- Task 2.2: IO Interceptor (terminal output)
- Task 2.4: IContextProvider (context injection)

### External Libraries
- System.Text.RegularExpressions (prompt parsing)
- System.IO (path operations)

---

## Estimated Complexity
- **TerminalPromptParser.cs**: Medium (~300 lines)
- **WorkingDirectoryTracker.cs**: Medium (~250 lines)
- **DirectoryChangedEventArgs.cs**: Low (~80 lines)

**Total Estimated:** ~630 lines of C# code

---

## Testing Strategy

### Unit Tests
- Prompt pattern matching
- Directory extraction
- Path expansion
- History stack operations

### Integration Tests
- Real-time detection from terminal output
- Context injection verification
- Event system integration

---

## Known Issues

### TODO Items
1. **Prompt Customization:**
   - User-defined prompt patterns
   - Regex pattern editor
   - Test mode for prompts

2. **Symlink Resolution:**
   - Resolve symbolic links to real paths
   - Handle broken symlinks

3. **Multiple Sessions:**
   - Per-session directory tracking
   - Session persistence

4. **Remote Directories:**
   - SSH session tracking
   - Remote path handling

---

## Mockup

```
Terminal Output:
user@hostname:~$ cd /home/user/project
user@hostname:/home/user/project$ pwd
/home/user/project

Parsed:
- Prompt Type: Bash
- Username: user
- Hostname: hostname
- Directory: /home/user/project (after cd)

Directory State:
Current: /home/user/project
History:
  /home/user/initial
  /home/user/project

Context Injection:
"Working directory: /home/user/project"
```

```
WorkingDirectoryTracker Usage:

var parser = new TerminalPromptParser();
var tracker = new WorkingDirectoryTracker(parser, logger);

// Parse terminal output
tracker.ParseTerminalOutput("user@hostname:~$ cd project");
// => Current directory updated to /home/user/project

// Get current directory
var dir = tracker.GetCurrentDirectory();
Console.WriteLine(dir); // /home/user/project

// Subscribe to changes
tracker.DirectoryChanged += (s, e) =>
{
    Console.WriteLine($"Changed from {e.OldDirectory} to {e.NewDirectory}");
};

// Push to history
tracker.Pushd();
tracker.ParseTerminalOutput("user@hostname:~$ cd oldproject");

// Pop from history
tracker.Popd();
```

