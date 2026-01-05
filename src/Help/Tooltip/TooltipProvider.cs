using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Help;

/// <summary>
/// Provides tooltip content for UI elements
/// </summary>
public class TooltipProvider
{
    private readonly Dictionary<string, TooltipData> _tooltips;
    private readonly Dictionary<string, List<InlineHelp>> _inlineHelp;
    private readonly List<ShortcutDefinition> _shortcuts;
    private readonly ILogger<TooltipProvider>? _logger;

    public TooltipProvider(ILogger<TooltipProvider>? logger = null)
    {
        _logger = logger;
        _tooltips = new Dictionary<string, TooltipData>(StringComparer.OrdinalIgnoreCase);
        _inlineHelp = new Dictionary<string, List<InlineHelp>>(StringComparer.OrdinalIgnoreCase);
        _shortcuts = new List<ShortcutDefinition>();
        InitializeContent();
        _logger?.LogInformation("TooltipProvider initialized with {TooltipCount} tooltips, {ShortcutCount} shortcuts",
            _tooltips.Count, _shortcuts.Count);
    }

    /// <summary>
    /// Gets a tooltip by ID
    /// </summary>
    public TooltipData? GetTooltip(string tooltipId)
    {
        return _tooltips.TryGetValue(tooltipId, out var tooltip) ? tooltip : null;
    }

    /// <summary>
    /// Gets tooltips for a target element
    /// </summary>
    public IEnumerable<TooltipData> GetTooltipsForTarget(string targetId)
    {
        return _tooltips.Values
            .Where(t => t.TargetId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Priority);
    }

    /// <summary>
    /// Gets inline help for a context
    /// </summary>
    public IEnumerable<InlineHelp> GetInlineHelp(string context)
    {
        return _inlineHelp.TryGetValue(context, out var help)
            ? help.OrderBy(h => h.Priority)
            : Enumerable.Empty<InlineHelp>();
    }

    /// <summary>
    /// Gets all shortcuts
    /// </summary>
    public IEnumerable<ShortcutDefinition> GetShortcuts()
    {
        return _shortcuts;
    }

    /// <summary>
    /// Gets shortcuts by category
    /// </summary>
    public IEnumerable<ShortcutDefinition> GetShortcutsByCategory(string category)
    {
        return _shortcuts.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets shortcuts for current platform
    /// </summary>
    public IEnumerable<ShortcutDefinition> GetShortcutsForCurrentPlatform()
    {
        var platform = GetCurrentPlatform();
        return _shortcuts.Where(s => s.Platform == platform || s.Platform == "All");
    }

    /// <summary>
    /// Gets keyboard shortcuts as Markdown
    /// </summary>
    public string GetShortcutsMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Keyboard Shortcuts");
        sb.AppendLine();

        var categories = _shortcuts.Select(s => s.Category).Distinct().OrderBy(c => c);

        foreach (var category in categories)
        {
            sb.AppendLine($"### {category}");
            sb.AppendLine();
            sb.AppendLine("| Action | Shortcut |");
            sb.AppendLine("|--------|----------|");

            foreach (var shortcut in GetShortcutsByCategory(category))
            {
                sb.AppendLine($"| {shortcut.Action} | `{shortcut.GetDisplayString()}` |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets quick reference card
    /// </summary>
    public string GetQuickReferenceCard()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Quick Reference");
        sb.AppendLine();

        sb.AppendLine("### Essential Shortcuts");
        sb.AppendLine("| Action | Windows/Linux | Mac |");
        sb.AppendLine("|--------|---------------|-----|");
        sb.AppendLine("| Send message | Ctrl+Enter | Cmd+Enter |");
        sb.AppendLine("| New line | Shift+Enter | Shift+Enter |");
        sb.AppendLine("| Previous message | Ctrl+Up | Cmd+Up |");
        sb.AppendLine("| Next message | Ctrl+Down | Cmd+Down |");
        sb.AppendLine();

        sb.AppendLine("### Slash Commands");
        sb.AppendLine("| Command | Description |");
        sb.AppendLine("|---------|-------------|");
        sb.AppendLine("| /help | Get help |");
        sb.AppendLine("| /clear | Clear chat |");
        sb.AppendLine("| /context | Manage context |");
        sb.AppendLine("| /status | Show status |");
        sb.AppendLine();

        sb.AppendLine("### Tips");
        sb.AppendLine("- Type `/` to see available commands");
        sb.AppendLine("- Use arrow keys for history");
        sb.AppendLine("- Press Tab for auto-completion");

        return sb.ToString();
    }

    private void InitializeContent()
    {
        InitializeTooltips();
        InitializeShortcuts();
        InitializeInlineHelp();
    }

    private void InitializeTooltips()
    {
        _tooltips["chat-input"] = new TooltipData
        {
            Id = "chat-input",
            Title = "Chat Input",
            Content = "Type your message here. Press Ctrl+Enter to send, Enter for new line.",
            Category = TooltipCategory.UI,
            TargetId = "chat-input",
            Priority = 100,
            ShowDelay = 300
        };

        _tooltips["slash-hint"] = new TooltipData
        {
            Id = "slash-hint",
            Title = "Slash Commands",
            Content = "Type / followed by a command. Try /help to see all commands.",
            Category = TooltipCategory.Command,
            TargetId = "chat-input",
            Priority = 90,
            ShowDelay = 1000
        };

        _tooltips["send-button"] = new TooltipData
        {
            Id = "send-button",
            Title = "Send Message",
            Content = "Send your message to the AI (Ctrl+Enter)",
            Category = TooltipCategory.Shortcut,
            TargetId = "send-button",
            Priority = 80
        };

        _tooltips["context-meter"] = new TooltipData
        {
            Id = "context-meter",
            Title = "Context Usage",
            Content = "Shows how much context is being used. Larger context = more history but slower.",
            Category = TooltipCategory.Tip,
            TargetId = "context-meter",
            Priority = 70
        };

        _tooltips["theme-toggle"] = new TooltipData
        {
            Id = "theme-toggle",
            Title = "Toggle Theme",
            Content = "Switch between light and dark mode",
            Category = TooltipCategory.UI,
            TargetId = "theme-toggle",
            Priority = 60
        };

        _tooltips["settings-button"] = new TooltipData
        {
            Id = "settings-button",
            Title = "Settings",
            Content = "Configure PairAdmin settings (/config)",
            Category = TooltipCategory.UI,
            TargetId = "settings-button",
            Priority = 50
        };
    }

    private void InitializeShortcuts()
    {
        _shortcuts = new List<ShortcutDefinition>
        {
            // Messaging
            new ShortcutDefinition
            {
                Id = "send",
                Action = "Send message",
                Platform = "Windows",
                Primary = "Ctrl+Enter",
                Secondary = "Ctrl+M",
                Category = "Messaging",
                Description = "Send your message to the AI"
            },
            new ShortcutDefinition
            {
                Id = "send-mac",
                Action = "Send message",
                Platform = "Mac",
                Primary = "Cmd+Enter",
                Secondary = "Cmd+M",
                Category = "Messaging",
                Description = "Send your message to the AI"
            },
            new ShortcutDefinition
            {
                Id = "new-line",
                Action = "Insert new line",
                Platform = "All",
                Primary = "Shift+Enter",
                Category = "Messaging",
                Description = "Insert a new line without sending"
            },
            new ShortcutDefinition
            {
                Id = "prev-message",
                Action = "Previous message",
                Platform = "Windows",
                Primary = "Ctrl+↑",
                Category = "Messaging",
                Description = "Recall previous message from history"
            },
            new ShortcutDefinition
            {
                Id = "prev-message-mac",
                Action = "Previous message",
                Platform = "Mac",
                Primary = "Cmd+↑",
                Category = "Messaging",
                Description = "Recall previous message from history"
            },
            new ShortcutDefinition
            {
                Id = "next-message",
                Action = "Next message",
                Platform = "Windows",
                Primary = "Ctrl+↓",
                Category = "Messaging",
                Description = "Recall next message from history"
            },
            new ShortcutDefinition
            {
                Id = "next-message-mac",
                Action = "Next message",
                Platform = "Mac",
                Primary = "Cmd+↓",
                Category = "Messaging",
                Description = "Recall next message from history"
            },

            // Navigation
            new ShortcutDefinition
            {
                Id = "home",
                Action = "Go to start",
                Platform = "All",
                Primary = "Home",
                Category = "Navigation",
                Description = "Go to start of conversation"
            },
            new ShortcutDefinition
            {
                Id = "end",
                Action = "Go to end",
                Platform = "All",
                Primary = "End",
                Category = "Navigation",
                Description = "Go to end of conversation"
            },
            new ShortcutDefinition
            {
                Id = "page-up",
                Action = "Page up",
                Platform = "All",
                Primary = "PgUp",
                Category = "Navigation",
                Description = "Scroll up one page"
            },
            new ShortcutDefinition
            {
                Id = "page-down",
                Action = "Page down",
                Platform = "All",
                Primary = "PgDn",
                Category = "Navigation",
                Description = "Scroll down one page"
            },

            // Commands
            new ShortcutDefinition
            {
                Id = "focus-command",
                Action = "Focus command bar",
                Platform = "All",
                Primary = "Ctrl+/",
                Category = "Commands",
                Description = "Focus the command input"
            },
            new ShortcutDefinition
            {
                Id = "help-command",
                Action = "Show help",
                Platform = "All",
                Primary = "F1",
                Category = "Commands",
                Description = "Open help documentation"
            },

            // Actions
            new ShortcutDefinition
            {
                Id = "copy",
                Action = "Copy selection",
                Platform = "Windows",
                Primary = "Ctrl+C",
                Category = "Actions",
                Description = "Copy selected text to clipboard"
            },
            new ShortcutDefinition
            {
                Id = "copy-mac",
                Action = "Copy selection",
                Platform = "Mac",
                Primary = "Cmd+C",
                Category = "Actions",
                Description = "Copy selected text to clipboard"
            },
            new ShortcutDefinition
            {
                Id = "select-all",
                Action = "Select all",
                Platform = "Windows",
                Primary = "Ctrl+A",
                Category = "Actions",
                Description = "Select all text"
            },
            new ShortcutDefinition
            {
                Id = "select-all-mac",
                Action = "Select all",
                Platform = "Mac",
                Primary = "Cmd+A",
                Category = "Actions",
                Description = "Select all text"
            }
        };
    }

    private void InitializeInlineHelp()
    {
        _inlineHelp["chat-start"] = new List<InlineHelp>
        {
            new InlineHelp
            {
                Id = "welcome-tip",
                Title = "Welcome!",
                Content = "Type a message to chat with the AI, or use `/help` to see available commands.",
                Category = HelpCategoryType.Tip,
                Priority = 100,
                Context = "chat-start",
                AutoDismiss = true,
                DismissDelay = 10
            }
        };

        _inlineHelp["first-command"] = new List<InlineHelp>
        {
            new InlineHelp
            {
                Id = "slash-intro",
                Title = "Slash Commands",
                Content = "Commands starting with `/` provide quick access to features. Try `/help`!",
                Category = HelpCategoryType.Info,
                Priority = 100,
                Context = "first-command",
                AutoDismiss = true,
                DismissDelay = 8
            }
        };

        _inlineHelp["context-low"] = new List<InlineHelp>
        {
            new InlineHelp
            {
                Id = "context-tip",
                Title = "Tip",
                Content = "Use `/context` to adjust how much conversation history is included.",
                Category = HelpCategoryType.Tip,
                Priority = 100,
                Context = "context-low",
                AutoDismiss = true,
                DismissDelay = 6
            }
        };

        _inlineHelp["filter-added"] = new List<InlineHelp>
        {
            new InlineHelp
            {
                Id = "filter-active",
                Title = "Filter Active",
                Content = "Your filter is now active. Sensitive data matching your pattern will be automatically redacted.",
                Category = HelpCategoryType.Success,
                Priority = 100,
                Context = "filter-added",
                AutoDismiss = true,
                DismissDelay = 5
            }
        };
    }

    private static string GetCurrentPlatform()
    {
        if (OperatingSystem.IsMacOS())
            return "Mac";
        if (OperatingSystem.IsLinux())
            return "Linux";
        return "Windows";
    }
}
