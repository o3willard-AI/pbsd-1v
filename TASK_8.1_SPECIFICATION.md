# Task 8.1 Specification: Enhanced Help System

## Task: Implement Enhanced Help System

**Phase:** Phase 8: Help & Documentation  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.2 (/help command) complete

---

## Description

Implement an enhanced help system that provides comprehensive, searchable, and context-aware documentation for PairAdmin. This includes a help service with tutorials, interactive help improvements, and a getting started guide.

---

## Deliverables

### 1. HelpService.cs
Core help service:
- Content management
- Search functionality
- Tutorial integration
- Context-aware help

### 2. HelpContent.cs
Help content models:
- Help topic structure
- Tutorial models
- Help category definitions
- Metadata support

### 3. HelpCommandHandler.cs (Enhanced)
Enhanced help command:
- Hierarchical help
- Tutorial access
- Search functionality
- Context suggestions

### 4. HelpTutorials.cs
Tutorial content:
- Getting started tutorial
- Advanced features tutorial
- Security best practices
- Tips and tricks

---

## Requirements

### Functional Requirements

#### Help Content
| Requirement | Description |
|-------------|-------------|
| Topic Organization | Hierarchical structure (Categories → Topics) |
| Search | Full-text search across all topics |
| Tags | Tag-based organization |
| Related Content | Cross-referenced topics |

#### Tutorials
| Requirement | Description |
|-------------|-------------|
| Step-by-step | Guided multi-step tutorials |
| Progress | Track tutorial completion |
| Interactive | Hands-on exercises |
| Quick Start | 5-minute getting started |

#### Enhanced Help Command
| Requirement | Description |
|-------------|-------------|
| Hierarchical | `/help` → `/help category` → `/help command` |
| Tutorials | `/help tutorial <name>` |
| Search | `/help search <query>` |
| Suggestions | Related commands and topics |

### Content Structure

```
Getting Started
├── Introduction
├── Installation
├── First Session
└── Configuration

Core Commands
├── help
├── clear
└── context

Configuration
├── model
├── theme
└── mode

Utility Commands
├── save-log
├── export
├── filter
└── status

Security
├── Sensitive Data Filtering
├── Command Validation
└── Audit Logging
```

---

## Implementation

### HelpContent Model

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Represents a help topic
/// </summary>
public class HelpTopic
{
    /// <summary>
    /// Topic ID (e.g., "getting-started.intro")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Topic title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Topic content (Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category ID
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Tags for search
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Related topic IDs
    /// </summary>
    public List<string> RelatedTopics { get; set; } = new();

    /// <summary>
    /// Priority for search results
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Minimum privilege level required
    /// </summary>
    public PrivilegeLevel RequiredLevel { get; set; } = PrivilegeLevel.Standard;
}

/// <summary>
/// Help category
/// </summary>
public class HelpCategory
{
    /// <summary>
    /// Category ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Icon name
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Topic IDs in this category
    /// </summary>
    public List<string> TopicIds { get; set; } = new();
}
```

### Tutorial Model

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Represents a tutorial
/// </summary>
public class Tutorial
{
    /// <summary>
    /// Tutorial ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tutorial title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level
    /// </summary>
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    /// <summary>
    /// Estimated duration in minutes
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// Tutorial steps
    /// </summary>
    public List<TutorialStep> Steps { get; set; } = new();

    /// <summary>
    /// Prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Tutorial step
/// </summary>
public class TutorialStep
{
    /// <summary>
    /// Step number
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Step title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Step content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Exercise to complete
    /// </summary>
    public string? Exercise { get; set; }

    /// <summary>
    /// Expected command to run
    /// </summary>
    public string? ExpectedCommand { get; set; }

    /// <summary>
    /// Hint for the step
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// Whether step is optional
    /// </summary>
    public bool IsOptional { get; set; }
}

public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced
}
```

### HelpService Class

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Service for managing help content
/// </summary>
public class HelpService
{
    private readonly Dictionary<string, HelpTopic> _topics;
    private readonly Dictionary<string, HelpCategory> _categories;
    private readonly Dictionary<string, Tutorial> _tutorials;
    private readonly ILogger<HelpService>? _logger;

    public HelpService(ILogger<HelpService>? logger = null)
    {
        _logger = logger;
        _topics = new Dictionary<string, HelpTopic>();
        _categories = new Dictionary<string, HelpCategory>();
        _tutorials = new Dictionary<string, Tutorial>();

        InitializeContent();
    }

    /// <summary>
    /// Gets a help topic by ID
    /// </summary>
    public HelpTopic? GetTopic(string topicId)
    {
        return _topics.TryGetValue(topicId, out var topic) ? topic : null;
    }

    /// <summary>
    /// Gets all topics in a category
    /// </summary>
    public IEnumerable<HelpTopic> GetTopicsByCategory(string categoryId)
    {
        return _topics.Values
            .Where(t => t.Category == categoryId)
            .OrderBy(t => t.Priority);
    }

    /// <summary>
    /// Searches help content
    /// </summary>
    public IEnumerable<HelpTopic> Search(string query)
    {
        var queryLower = query.ToLowerInvariant();
        return _topics.Values
            .Where(t =>
                t.Title.ToLowerInvariant().Contains(queryLower) ||
                t.Content.ToLowerInvariant().Contains(queryLower) ||
                t.Tags.Any(tag => tag.ToLowerInvariant().Contains(queryLower)))
            .OrderByDescending(t => t.Priority);
    }

    /// <summary>
    /// Gets a tutorial by ID
    /// </summary>
    public Tutorial? GetTutorial(string tutorialId)
    {
        return _tutorials.TryGetValue(tutorialId, out var tutorial) ? tutorial : null;
    }

    /// <summary>
    /// Gets all tutorials
    /// </summary>
    public IEnumerable<Tutorial> GetTutorials()
    {
        return _tutorials.Values.OrderBy(t => t.Difficulty);
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    public IEnumerable<HelpCategory> GetCategories()
    {
        return _categories.Values.OrderBy(c => c.Order);
    }

    /// <summary>
    /// Gets related topics
    /// </summary>
    public IEnumerable<HelpTopic> GetRelatedTopics(string topicId)
    {
        var topic = GetTopic(topicId);
        if (topic == null)
            return Enumerable.Empty<HelpTopic>();

        return topic.RelatedTopics
            .Select(GetTopic)
            .Where(t => t != null)!;
    }

    private void InitializeContent()
    {
        InitializeCategories();
        InitializeTopics();
        InitializeTutorials();
    }

    private void InitializeCategories() { /* ... */ }
    private void InitializeTopics() { /* ... */ }
    private void InitializeTutorials() { /* ... */ }
}
```

---

## Help Content Examples

### Getting Started Topic

```markdown
# Getting Started with PairAdmin

PairAdmin is an AI-assisted terminal administration extension for PuTTY.

## What You'll Need

- PuTTY terminal emulator
- An OpenAI API key
- A running terminal session

## Quick Start

1. Configure your API key using `/model`
2. Start chatting with the AI
3. Use slash commands for quick actions

## Next Steps

- Learn about [Core Commands](core-commands)
- Explore [Configuration Options](configuration)
- Read [Security Best Practices](security)
```

### Tutorial Step

```markdown
## Step 1: Your First Command

Let's start by exploring the help system.

### Exercise
Run the `/help` command to see all available commands.

### Expected Command
```
/help
```

### Hint
The `/help` command shows all available commands organized by category.
```

---

## Integration Points

### With HelpCommandHandler (Task 6.2)
```csharp
// Enhanced /help command
var helpService = new HelpService();
var topic = helpService.GetTopic("getting-started");
var response = RenderHelpTopic(topic);
```

### With ChatPane (Task 3.1)
```csharp
// Show help in chat
helpService.GetRelatedTopics(currentTopic).ToList()
    .ForEach(topic => chatPane.AddHelpLink(topic));
```

### With CommandDispatcher (Task 6.1)
```csharp
// Context-sensitive help
var relatedTopics = helpService.GetRelatedTopics(currentCommand);
```

---

## Acceptance Criteria

- [ ] Help topics organized by category
- [ ] Full-text search works
- [ ] Tutorials with steps
- [ ] Progress tracking
- [ ] Related topics displayed
- [ ] Search by tags
- [ ] Context-aware suggestions
- [ ] Markdown rendering
- [ ] Difficulty levels
- [ ] Estimated durations

---

## Files Created

```
src/Help/
├── HelpService.cs              # Main service (200 lines)
├── HelpContent.cs              # Content models (150 lines)
├── HelpTutorials.cs            # Tutorial content (200 lines)
└── HelpCommandHandler.cs       # Enhanced /help (150 lines)

src/Help/Content/
├── GettingStarted.md           # Quick start guide
├── CoreCommands.md             # Command reference
├── Configuration.md            # Settings guide
└── Security.md                 # Security guide
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| HelpService.cs | Medium | ~200 |
| HelpContent.cs | Low | ~150 |
| HelpTutorials.cs | Medium | ~200 |
| HelpCommandHandler.cs | Low | ~150 |

**Total Estimated:** ~700 lines of C#

---

## Next Steps

After Task 8.1 is complete:
1. Task 8.2: Command Reference
2. Task 8.3: Tooltip Help
3. Phase 8 Complete Summary

---

## Notes

- Use Markdown for all help content
- Include examples in every topic
- Add keyboard shortcuts where applicable
- Keep tutorials under 15 minutes
- Include progress indicators
