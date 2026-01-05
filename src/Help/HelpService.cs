using Microsoft.Extensions.Logging;

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
        _topics = new Dictionary<string, HelpTopic>(StringComparer.OrdinalIgnoreCase);
        _categories = new Dictionary<string, HelpCategory>(StringComparer.OrdinalIgnoreCase);
        _tutorials = new Dictionary<string, Tutorial>(StringComparer.OrdinalIgnoreCase);

        InitializeContent();
        _logger?.LogInformation("HelpService initialized with {TopicCount} topics, {CategoryCount} categories, {TutorialCount} tutorials",
            _topics.Count, _categories.Count, _tutorials.Count);
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
            .Where(t => t.Category.Equals(categoryId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Priority);
    }

    /// <summary>
    /// Gets topics by category name
    /// </summary>
    public IEnumerable<HelpTopic> GetTopicsByCategoryName(string categoryName)
    {
        var category = _categories.Values
            .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        
        if (category == null)
            return Enumerable.Empty<HelpTopic>();

        return GetTopicsByCategory(category.Id);
    }

    /// <summary>
    /// Searches help content
    /// </summary>
    public IEnumerable<HelpTopic> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<HelpTopic>();

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
        return _tutorials.Values.OrderBy(t => (int)t.Difficulty).ThenBy(t => t.EstimatedMinutes);
    }

    /// <summary>
    /// Gets tutorials by difficulty
    /// </summary>
    public IEnumerable<Tutorial> GetTutorialsByDifficulty(TutorialDifficulty difficulty)
    {
        return _tutorials.Values
            .Where(t => t.Difficulty == difficulty)
            .OrderBy(t => t.EstimatedMinutes);
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
            .Where(t => t != null)
            .Cast<HelpTopic>();
    }

    /// <summary>
    /// Gets command help for a specific command
    /// </summary>
    public HelpTopic? GetCommandHelp(string commandName)
    {
        var normalizedName = commandName.TrimStart('/').ToLowerInvariant();
        return GetTopic($"command.{normalizedName}");
    }

    /// <summary>
    /// Gets all command topics
    /// </summary>
    public IEnumerable<HelpTopic> GetCommandTopics()
    {
        return _topics.Values
            .Where(t => t.Id.StartsWith("command.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Title);
    }

    /// <summary>
    /// Gets the getting started topic
    /// </summary>
    public HelpTopic? GetGettingStarted()
    {
        return GetTopic("getting-started.intro");
    }

    /// <summary>
    /// Gets topic count
    /// </summary>
    public int GetTopicCount() => _topics.Count;

    /// <summary>
    /// Gets tutorial count
    /// </summary>
    public int GetTutorialCount() => _tutorials.Count;

    private void InitializeContent()
    {
        InitializeCategories();
        InitializeTopics();
        InitializeTutorials();
    }

    private void InitializeCategories()
    {
        _categories["getting-started"] = new HelpCategory
        {
            Id = "getting-started",
            Name = "Getting Started",
            Description = "New to PairAdmin? Start here!",
            Icon = "Rocket",
            Order = 1,
            TopicIds = new List<string>
            {
                "getting-started.intro",
                "getting-started.quick-start",
                "getting-started.first-session",
                "getting-started.configuration"
            }
        };

        _categories["core-commands"] = new HelpCategory
        {
            Id = "core-commands",
            Name = "Core Commands",
            Description = "Essential commands for daily use",
            Icon = "Command",
            Order = 2,
            TopicIds = new List<string>
            {
                "command.help",
                "command.clear",
                "command.context"
            }
        };

        _categories["configuration"] = new HelpCategory
        {
            Id = "configuration",
            Name = "Configuration",
            Description = "Configure PairAdmin to your needs",
            Icon = "Settings",
            Order = 3,
            TopicIds = new List<string>
            {
                "command.model",
                "command.theme",
                "command.mode"
            }
        };

        _categories["utility"] = new HelpCategory
        {
            Id = "utility",
            Name = "Utility Commands",
            Description = "Helpful utilities for daily tasks",
            Icon = "Tools",
            Order = 4,
            TopicIds = new List<string>
            {
                "command.save-log",
                "command.export",
                "command.filter",
                "command.status",
                "command.config",
                "command.quit"
            }
        };

        _categories["security"] = new HelpCategory
        {
            Id = "security",
            Name = "Security",
            Description = "Keep your data safe",
            Icon = "Shield",
            Order = 5,
            TopicIds = new List<string>
            {
                "security.intro",
                "security.sensitive-data",
                "security.validation",
                "security.audit"
            }
        };

        _categories["tips"] = new HelpCategory
        {
            Id = "tips",
            Name = "Tips & Tricks",
            Description = "Get the most out of PairAdmin",
            Icon = "Lightbulb",
            Order = 6,
            TopicIds = new List<string>
            {
                "tips.productivity",
                "tips.customization",
                "tips.troubleshooting"
            }
        };
    }

    private void InitializeTopics()
    {
        _topics["getting-started.intro"] = new HelpTopic
        {
            Id = "getting-started.intro",
            Title = "Introduction to PairAdmin",
            Category = "getting-started",
            Priority = 100,
            Tags = new List<string> { "intro", "overview", "what is" },
            Content = @"# Welcome to PairAdmin

PairAdmin is an AI-assisted terminal administration extension for PuTTY. It helps you manage your terminal sessions more efficiently with the power of AI.

## What PairAdmin Does

- **AI-Powered Assistance**: Get intelligent help with terminal commands
- **Context Awareness**: Remembers your working directory and session context
- **Slash Commands**: Quick actions with `/` commands
- **Secure by Design**: Sensitive data is automatically detected and filtered

## Key Features

1. **Smart Terminal**: AI that understands your terminal context
2. **Slash Commands**: Quick actions like `/help`, `/clear`, `/context`
3. **Context Management**: Automatically tracks your working directory
4. **Security First**: Sensitive data is always protected

## Getting Help

- Type `/help` to see all commands
- Type `/help <command>` for specific command help
- Use `/help tutorial` to start an interactive tutorial

## Next Steps

- [Quick Start Guide](getting-started.quick-start)
- [Core Commands](core-commands)
- [Configuration](configuration)"
        };

        _topics["getting-started.quick-start"] = new HelpTopic
        {
            Id = "getting-started.quick-start",
            Title = "Quick Start Guide",
            Category = "getting-started",
            Priority = 90,
            Tags = new List<string> { "quick", "start", "setup" },
            Content = @"# Quick Start Guide

Get up and running with PairAdmin in 5 minutes!

## Step 1: Configure Your LLM

PairAdmin needs an LLM provider to generate responses:

```
/model --provider openai
```

You'll need an OpenAI API key. See [Configuration](getting-started.configuration) for details.

## Step 2: Try a Simple Command

Start with the help command:

```
/help
```

## Step 3: Ask a Question

Try asking the AI a question about terminal commands:

```
What's the command to list files recursively in Linux?
```

## Step 4: Use Context Commands

See your current context settings:

```
/context
```

## Next Steps

- Complete the [First Session](getting-started.first-session) tutorial
- Explore [Core Commands](core-commands)
- Learn about [Security Features](security.intro)"
        };

        _topics["command.help"] = new HelpTopic
        {
            Id = "command.help",
            Title = "/help - Display Help",
            Category = "core-commands",
            Priority = 100,
            Tags = new List<string> { "help", "commands", "guide" },
            Content = @"# /help Command

Displays help information for commands and topics.

## Syntax

```
/help
/help <command>
/help <category>
/help search <query>
```

## Examples

```/help                    # Show all commands
/help context           # Get help for /context command
/help core-commands     # Show core commands category
/help search model      # Search for 'model' related help
```

## Aliases

- `/h` - Short form
- `/?` - Alternative form

## Related Commands

- `/context` - Manage context window
- `/clear` - Clear chat history
- `/status` - Show system status

## Tips

Use `/help search <term>` to find help topics related to a specific term."
        };

        _topics["command.clear"] = new HelpTopic
        {
            Id = "command.clear",
            Title = "/clear - Clear Chat History",
            Category = "core-commands",
            Priority = 90,
            Tags = new List<string> { "clear", "reset", "chat" },
            Content = @"# /clear Command

Clears chat history and resets the conversation.

## Syntax

```
/clear           # Clear everything (messages + context)
/clear messages  # Clear only messages
/clear context   # Clear only context
```

## Examples

```/clear              # Start fresh
/clear messages     # Keep context, clear messages
/clear context      # Keep messages, clear context
```

## Aliases

- `/cls` - Windows-style
- `/reset` - Alternative

## What Gets Cleared

| Option | Messages | Context | Settings |
|--------|----------|---------|----------|
| `/clear` | ✅ | ✅ | ❌ |
| `/clear messages` | ✅ | ❌ | ❌ |
| `/clear context` | ❌ | ✅ | ❌ |

## Related Commands

- `/help` - Get help
- `/context` - Manage context
- `/save-log` - Export before clearing"
        };

        _topics["command.context"] = new HelpTopic
        {
            Id = "command.context",
            Title = "/context - Manage Context Window",
            Category = "core-commands",
            Priority = 85,
            Tags = new List<string> { "context", "window", "memory" },
            Content = @"# /context Command

Manages the context window size and behavior.

## Syntax

```
/context                    # Show current settings
/context 100                # Set to 100 lines
/context 50%                # Set to 50% of maximum
/context --percentage 75    # Set to 75%
/context --auto             # Enable auto-sizing
```

## Examples

```/context              # View current settings
/context 200           # 200 lines
/context 75%           # 75% of max
/context --auto        # Let AI decide
```

## Context Modes

| Mode | Description |
|------|-------------|
| Auto | Use model defaults |
| Fixed | Exact line count |
| Percentage | % of maximum |

## Related Commands

- `/help` - Get help
- `/clear context` - Clear context
- `/filter` - Manage data filters

## Tips

- Smaller context = faster responses
- Larger context = more history but slower
- Auto mode is recommended for most users"
        };

        _topics["security.sensitive-data"] = new HelpTopic
        {
            Id = "security.sensitive-data",
            Title = "Sensitive Data Filtering",
            Category = "security",
            Priority = 90,
            Tags = new List<string> { "security", "filter", "sensitive", "api keys" },
            Content = @"# Sensitive Data Filtering

PairAdmin automatically detects and redacts sensitive data from terminal output before sending it to the AI.

## What Gets Filtered

- **API Keys**: AWS, OpenAI, GitHub, Stripe, etc.
- **Passwords**: Password assignments and secrets
- **Private Keys**: SSH, RSA, PGP keys
- **Credit Cards**: Card numbers
- **Emails**: Email addresses
- **Tokens**: JWT, Bearer tokens

## Redaction Methods

| Method | Example |
|--------|---------|
| Mask | `****abcd` |
| Remove | (empty) |
| Hash | `#hash:a1b2c3d4` |
| Placeholder | `[REDACTED]` |

## Custom Filters

Use the `/filter` command to add your own patterns:

```/filter add "my-secret-key:"
/filter --regex "(?i)password.*"
/filter list              # Show all filters
/filter clear             # Remove all
```

## Related Commands

- `/filter` - Manage filters
- `/status` - Check filter status
- `/help security` - More security info"
        };

        _topics["tips.productivity"] = new HelpTopic
        {
            Id = "tips.productivity",
            Title = "Productivity Tips",
            Category = "tips",
            Priority = 80,
            Tags = new List<string> { "tips", "productivity", "效率" },
            Content = @"# Productivity Tips

Get more done with PairAdmin!

## Keyboard Shortcuts

- `Ctrl+Enter` - Send message
- `Ctrl+Up` - Previous message
- `Ctrl+Down` - Next message
- `/` - Start slash command

## Efficient Workflows

### Quick Help
```
/help <command>    # Direct command help
```

### Context Management
```
/context           # Check settings
/context 100       # Set lines
/context --auto    # Auto mode
```

### Bulk Operations
```
/filter add "api_key:"     # Add pattern
/filter add "password:"    # Another pattern
```

## Best Practices

1. **Use Context Wisely**
   - Start with auto mode
   - Increase for long conversations
   - Decrease for quick queries

2. **Leverage Filters**
   - Add common sensitive patterns
   - Review filters periodically
   - Use regex for complex patterns

3. **Save Important Work**
   ```
   /save-log           # Save session
   /export --copy      # Copy chat
   ```
"
        };
    }

    private void InitializeTutorials()
    {
        _tutorials["getting-started"] = new Tutorial
        {
            Id = "getting-started",
            Title = "Getting Started with PairAdmin",
            Description = "Learn the basics of PairAdmin in 5 minutes",
            Difficulty = TutorialDifficulty.Beginner,
            EstimatedMinutes = 5,
            Tags = new List<string> { "basics", "intro", "start" },
            Prerequisites = new List<string>(),
            Steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    StepNumber = 1,
                    Title = "Explore the Help System",
                    Content = "Let's start by exploring what PairAdmin can do.",
                    Exercise = "Run the /help command to see all available commands.",
                    ExpectedCommand = "/help",
                    Hint = "Just type /help and press Enter"
                },
                new TutorialStep
                {
                    StepNumber = 2,
                    Title = "Check Your Context",
                    Content = "See how PairAdmin tracks your conversation context.",
                    Exercise = "Run /context to see current settings.",
                    ExpectedCommand = "/context",
                    Hint = "The /context command shows your current settings"
                },
                new TutorialStep
                {
                    StepNumber = 3,
                    Title = "Ask a Question",
                    Content = "Try asking the AI a question about terminal commands.",
                    Exercise = "Ask: 'How do I list files by size?'",
                    ExpectedCommand = null,
                    Hint = "Just type a regular question, no slash needed"
                },
                new TutorialStep
                {
                    StepNumber = 4,
                    Title = "Save Your Work",
                    Content = "Learn how to export your session.",
                    Exercise = "Run /help save-log to learn about saving logs.",
                    ExpectedCommand = "/help save-log",
                    Hint = "Use /help to learn about other commands"
                },
                new TutorialStep
                {
                    StepNumber = 5,
                    Title = "Complete!",
                    Content = "You've completed the getting started tutorial!",
                    Exercise = "Explore more with /help or try the next tutorial.",
                    ExpectedCommand = null,
                    Hint = "Try /help tutorial to see all tutorials"
                }
            }
        };

        _tutorials["context-management"] = new Tutorial
        {
            Id = "context-management",
            Title = "Mastering Context Management",
            Description = "Learn to optimize context for better AI responses",
            Difficulty = TutorialDifficulty.Intermediate,
            EstimatedMinutes = 10,
            Tags = new List<string> { "context", "advanced", "optimization" },
            Prerequisites = new List<string> { "getting-started" },
            Steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    StepNumber = 1,
                    Title = "Understanding Context",
                    Content = "Learn what context means and why it matters.",
                    Exercise = "Run /context to see current settings.",
                    ExpectedCommand = "/context",
                    Hint = "This shows your current context configuration"
                },
                new TutorialStep
                {
                    StepNumber = 2,
                    Title = "Setting Context Size",
                    Content = "Learn to adjust context for different needs.",
                    Exercise = "Try /context 200 to set 200 lines.",
                    ExpectedCommand = "/context 200",
                    Hint = "The number represents maximum lines"
                },
                new TutorialStep
                {
                    StepNumber = 3,
                    Title = "Using Percentage Mode",
                    Content = "Set context as a percentage of maximum.",
                    Exercise = "Try /context 50% for half of max.",
                    ExpectedCommand = "/context 50%",
                    Hint = "Use the % symbol for percentage mode"
                },
                new TutorialStep
                {
                    StepNumber = 4,
                    Title = "Auto Mode",
                    Content = "Let the AI decide the best context size.",
                    Exercise = "Try /context --auto.",
                    ExpectedCommand = "/context --auto",
                    Hint = "Auto mode uses model defaults"
                },
                new TutorialStep
                {
                    StepNumber = 5,
                    Title = "Tutorial Complete",
                    Content = "You now understand context management!",
                    Exercise = "Experiment with different settings.",
                    ExpectedCommand = null,
                    Hint = "Try different values to see what works best"
                }
            }
        };
    }
}
