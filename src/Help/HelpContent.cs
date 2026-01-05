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
    public Security.PrivilegeLevel RequiredLevel { get; set; } = Security.PrivilegeLevel.Standard;

    /// <summary>
    /// Gets a summary of the topic
    /// </summary>
    public string GetSummary()
    {
        var firstLine = Content.Split('\n')
            .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        return firstLine.Length > 150 ? firstLine[..150] + "..." : firstLine;
    }
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
    public string Icon { get; set; } = "Help";

    /// <summary>
    /// Display order
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Topic IDs in this category
    /// </summary>
    public List<string> TopicIds { get; set; } = new();
}

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
    public TutorialDifficulty Difficulty { get; set; } = TutorialDifficulty.Beginner;

    /// <summary>
    /// Estimated duration in minutes
    /// </summary>
    public int EstimatedMinutes { get; set; }

    /// <summary>
    /// Tutorial steps
    /// </summary>
    public List<TutorialStep> Steps { get; set; } = new();

    /// <summary>
    /// Prerequisites (topic IDs)
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps => Steps.Count;

    /// <summary>
    /// Gets a formatted duration string
    /// </summary>
    public string GetDurationString()
    {
        if (EstimatedMinutes < 60)
            return $"{EstimatedMinutes} min";
        var hours = EstimatedMinutes / 60;
        var mins = EstimatedMinutes % 60;
        return mins > 0 ? $"{hours}h {mins}m : $"{hours}h";
    }
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

    /// <summary>
    /// Gets step identifier
    /// </summary>
    public string GetStepId() => $"step-{StepNumber}";
}

/// <summary>
/// Tutorial difficulty level
/// </summary>
public enum TutorialDifficulty
{
    /// <summary>
    /// Suitable for new users
    /// </summary>
    Beginner,

    /// <summary>
    /// Requires some experience
    /// </summary>
    Intermediate,

    /// <summary>
    /// Advanced users only
    /// </summary>
    Advanced
}
