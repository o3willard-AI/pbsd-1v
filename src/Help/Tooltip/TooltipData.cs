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

    /// <summary>
    /// Icon to display
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Creates a formatted tooltip string
    /// </summary>
    public string GetFormattedContent()
    {
        if (!string.IsNullOrEmpty(Icon))
        {
            return $"[{Icon}] {Content}";
        }
        return Content;
    }
}

/// <summary>
/// Tooltip category
/// </summary>
public enum TooltipCategory
{
    /// <summary>
    /// Command-related help
    /// </summary>
    Command,

    /// <summary>
    /// Keyboard shortcut
    /// </summary>
    Shortcut,

    /// <summary>
    /// UI element hint
    /// </summary>
    UI,

    /// <summary>
    /// Feature tip
    /// </summary>
    Tip,

    /// <summary>
    /// Caution/warning
    /// </summary>
    Warning,

    /// <summary>
    /// Error/recovery help
    /// </summary>
    Error,

    /// <summary>
    /// General information
    /// </summary>
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
    /// Platform: Windows, Mac, Linux, All
    /// </summary>
    public string Platform { get; set; } = "All";

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

    /// <summary>
    /// Gets the display shortcut string
    /// </summary>
    public string GetDisplayString()
    {
        if (!string.IsNullOrEmpty(Secondary))
        {
            return $"{Primary} / {Secondary}";
        }
        return Primary;
    }
}

/// <summary>
/// Inline help message
/// </summary>
public class InlineHelp
{
    /// <summary>
    /// Help ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Help title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Help content (Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Help category
    /// </summary>
    public HelpCategoryType Category { get; set; } = HelpCategoryType.Info;

    /// <summary>
    /// Priority for display order
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Context where this help applies
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Whether to auto-dismiss
    /// </summary>
    public bool AutoDismiss { get; set; }

    /// <summary>
    /// Dismiss delay in seconds
    /// </summary>
    public int DismissDelay { get; set; } = 5;
}

/// <summary>
/// Inline help category
/// </summary>
public enum HelpCategoryType
{
    Tip,
    Info,
    Warning,
    Error,
    Success
}
