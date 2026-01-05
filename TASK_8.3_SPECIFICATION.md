# Task 8.3 Specification: Tooltip Help System

## Task: Implement Tooltip Help System

**Phase:** Phase 8: Help & Documentation  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 8.1 and 8.2 complete

---

## Description

Implement a tooltip help system that provides contextual help hints for UI elements, keyboard shortcuts, and quick reference information.

---

## Deliverables

### 1. TooltipProvider.cs
Tooltip content provider:
- Tooltip data for UI elements
- Keyboard shortcut tooltips
- Inline help messages
- Quick reference cards

### 2. ShortcutHelp.cs
Keyboard shortcut definitions:
- All shortcuts documented
- Categorized by function
- Platform-specific variants
- Customizable bindings

### 3. InlineHelp.cs
Inline help messages:
- Contextual hints
- Feature tips
- Warning messages
- Suggestions

---

## Requirements

### Tooltip Categories

| Category | Description | Examples |
|----------|-------------|----------|
| Command | Slash command help | "/help - Get help" |
| Shortcut | Keyboard shortcuts | "Ctrl+Enter - Send" |
| UI | UI element hints | "Send message" |
| Tip | Feature tips | "Try /context for more history" |
| Warning | Caution messages | "This will clear all messages" |
| Error | Error recovery | "Check your API key" |

### Shortcut Categories

| Category | Shortcuts |
|----------|-----------|
| Messaging | Ctrl+Enter, Ctrl+Up/Down |
| Commands | /, Tab completion |
| Navigation | Home, End, PgUp/PgDn |
| Actions | Ctrl+S (save), Ctrl+N (new) |

---

## Implementation

### TooltipData Model

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Represents a tooltip
/// </summary>
public class TooltipData
{
    /// <summary>
    /// Tooltip ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Short title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Tooltip content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public TooltipCategory Category { get; set; }

    /// <summary>
    /// Target element ID
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Priority (higher = more important)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether to show on hover
    /// </summary>
    public bool ShowOnHover { get; set; } = true;

    /// <summary>
    /// Whether to show on focus
    /// </summary>
    public bool ShowOnFocus { get; set; } = true;

    /// <summary>
    /// Delay before showing (ms)
    /// </summary>
    public int ShowDelay { get; set; } = 500;

    /// <summary>
    /// Maximum width
    /// </summary>
    public int MaxWidth { get; set; } = 300;
}

/// <summary>
/// Tooltip category
/// </summary>
public enum TooltipCategory
{
    Command,
    Shortcut,
    UI,
    Tip,
    Warning,
    Error,
    Info
}

/// <summary>
/// Keyboard shortcut definition
/// </summary>
public class ShortcutDefinition
{
    /// <summary>
    /// Shortcut ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Action description
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Platform: Windows, Mac, Linux
    /// </summary>
    public string Platform { get; set; } = "Windows";

    /// <summary>
    /// Primary shortcut
    /// </summary>
    public string Primary { get; set; } = string.Empty;

    /// <summary>
    /// Secondary shortcut (if different)
    /// </summary>
    public string? Secondary { get; set; }

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether it's a global shortcut
    /// </summary>
    public bool IsGlobal { get; set; }
}
```

### TooltipProvider Class

```csharp
namespace PairAdmin.Help;

/// <summary>
/// Provides tooltip content for UI elements
/// </summary>
public class TooltipProvider
{
    private readonly Dictionary<string, TooltipData> _tooltips;
    private readonly List<ShortcutDefinition> _shortcuts;
    private readonly ILogger<TooltipProvider>? _logger;

    public TooltipProvider(ILogger<TooltipProvider>? logger = null)
    {
        _logger = logger;
        _tooltips = new Dictionary<string, TooltipData>(StringComparer.OrdinalIgnoreCase);
        _shortcuts = new List<ShortcutDefinition>();
        InitializeTooltips();
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
    public IEnumerable<ShortcutDefinition> GetShortcutsForPlatform()
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

        var categories = _shortcuts.Select(s => s.Category).Distinct();

        foreach (var category in categories)
        {
            sb.AppendLine($"### {category}");
            sb.AppendLine();
            sb.AppendLine("| Action | Shortcut |");
            sb.AppendLine("|--------|----------|");

            foreach (var shortcut in GetShortcutsByCategory(category))
            {
                sb.AppendLine($"| {shortcut.Action} | `{shortcut.Primary}` |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void InitializeTooltips()
    {
        InitializeCommandTooltips();
        InitializeUITooltips();
        InitializeShortcutTooltips();
    }

    private void InitializeCommandTooltips()
    {
        _tooltips["send-message"] = new TooltipData
        {
            Id = "send-message",
            Title = "Send Message",
            Content = "Press Ctrl+Enter to send your message to the AI",
            Category = TooltipCategory.Shortcut,
            TargetId = "chat-input",
            Priority = 100
        };

        _tooltips["slash-commands"] = new TooltipData
        {
            Id = "slash-commands",
            Title = "Slash Commands",
            Content = "Type / followed by a command for quick actions. Try /help to see all commands.",
            Category = TooltipCategory.Command,
            TargetId = "chat-input",
            Priority = 90
        };
    }

    private void InitializeUITooltips() { /* ... */ }
    private void InitializeShortcutTooltips() { /* ... */ }

    private static string GetCurrentPlatform()
    {
        if (OperatingSystem.IsMacOS())
            return "Mac";
        if (OperatingSystem.IsLinux())
            return "Linux";
        return "Windows";
    }
}
```

---

## Integration Points

### With ChatPane (Task 3.1)
```csharp
// Add tooltips to chat input
var tooltip = tooltipProvider.GetTooltip("slash-commands");
chatInput.ToolTip = tooltip.Content;
```

### With MainWindow
```csharp
// Show shortcut help
shortcutsHelp.Text = tooltipProvider.GetShortcutsMarkdown();
```

### With Settings Pane
```csharp
// Contextual help for settings
var helpText = tooltipProvider.GetTooltip($"setting-{settingId}")?.Content;
```

---

## Acceptance Criteria

- [ ] Tooltips for all slash commands
- [ ] Keyboard shortcuts documented
- [ ] Platform-specific shortcuts
- [ ] Tooltips for UI elements
- [ ] Contextual inline help
- [ ] Quick reference cards
- [ ] Searchable shortcuts
- [ ] Markdown export

---

## Files Created

```
src/Help/Tooltip/
├── TooltipProvider.cs          # Main provider (200 lines)
├── TooltipData.cs              # Data models (100 lines)
├── ShortcutHelp.cs             # Shortcut definitions (150 lines)
└── InlineHelp.cs               # Inline help messages (100 lines)
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| TooltipProvider.cs | Low | ~200 |
| TooltipData.cs | Low | ~100 |
| ShortcutHelp.cs | Low | ~150 |
| InlineHelp.cs | Low | ~100 |

**Total Estimated:** ~550 lines of C#

---

## Next Steps

After Task 8.3 is complete:
1. Phase 8 Complete Summary

---

## Notes

- Use consistent tooltip styling
- Include examples in tooltips
- Support keyboard navigation
- Allow tooltip customization
- Consider animation effects
