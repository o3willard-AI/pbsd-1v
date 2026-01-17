namespace PairAdmin.Commands;

/// <summary>
/// Command metadata
/// </summary>
public class CommandMetadata
{
    /// <summary>
    /// Command name (e.g., "help", "clear")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Brief description of the command
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Usage syntax (e.g., "/command arg1 arg2")
    /// </summary>
    public string Syntax { get; set; }

    /// <summary>
    /// Examples of command usage
    /// </summary>
    public List<string> Examples { get; set; }

    /// <summary>
    /// Command category (Core, Configuration, Utility)
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Whether the command is currently available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Minimum number of arguments
    /// </summary>
    public int MinArguments { get; set; }

    /// <summary>
    /// Maximum number of arguments (-1 for unlimited)
    /// </summary>
    public int MaxArguments { get; set; }

    /// <summary>
    /// Aliases for the command
    /// </summary>
    public List<string> Aliases { get; set; }

    /// <summary>
    /// Permissions required to run the command
    /// </summary>
    public List<string> RequiredPermissions { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public CommandMetadata()
    {
        Name = string.Empty;
        Description = string.Empty;
        Syntax = string.Empty;
        Examples = new List<string>();
        Category = "General";
        IsAvailable = true;
        MinArguments = 0;
        MaxArguments = -1;
        Aliases = new List<string>();
        RequiredPermissions = new List<string>();
    }

    /// <summary>
    /// Creates a summary of the command
    /// </summary>
    public string GetSummary()
    {
        return $"{Name} - {Description}";
    }

    /// <summary>
    /// Creates full help text
    /// </summary>
    public string GetHelpText()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"## /{Name}");
        sb.AppendLine();
        sb.AppendLine(Description);
        sb.AppendLine();
        sb.AppendLine("**Usage:**");
        sb.AppendLine($"```\n{Syntax}\n```");
        sb.AppendLine();

        if (Examples.Count > 0)
        {
            sb.AppendLine("**Examples:**");
            foreach (var example in Examples)
            {
                sb.AppendLine($"- {example}");
            }
            sb.AppendLine();
        }

        if (Aliases.Count > 0)
        {
            sb.AppendLine($"**Aliases:** {string.Join(", ", Aliases.Select(a => $"/{a}"))}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Context for command execution
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Current chat messages
    /// </summary>
    public System.Collections.Generic.IList<Chat.ChatMessage> Messages { get; set; }

    /// <summary>
    /// Current user settings
    /// </summary>
    public Configuration.UserSettings Settings { get; set; }

    /// <summary>
    /// LLM Gateway for model operations
    /// </summary>
    public LLMGateway.LLMGateway? LLMGateway { get; set; }

    /// <summary>
    /// Context window manager
    /// </summary>
    public Context.ContextWindowManager? ContextManager { get; set; }

    /// <summary>
    /// Chat pane for UI operations (typed as object to avoid circular dependency)
    /// </summary>
    public object? ChatPane { get; set; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public System.Threading.CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public CommandContext()
    {
        Messages = new System.Collections.Generic.List<Chat.ChatMessage>();
        Settings = new Configuration.UserSettings();
        CancellationToken = System.Threading.CancellationToken.None;
    }
}
