using System.Text;
using System.Text.Json.Serialization;

namespace PairAdmin.Help;

/// <summary>
/// Complete command reference documentation
/// </summary>
public class CommandReference
{
    /// <summary>
    /// Command name (without /)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full command with /
    /// </summary>
    [JsonIgnore]
    public string Command => "/" + Name;

    /// <summary>
    /// Short description
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    [JsonPropertyName("detailedDescription")]
    public string DetailedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Syntax example
    /// </summary>
    [JsonPropertyName("syntax")]
    public string Syntax { get; set; } = string.Empty;

    /// <summary>
    /// Usage examples
    /// </summary>
    [JsonPropertyName("examples")]
    public List<string> Examples { get; set; } = new();

    /// <summary>
    /// Command arguments
    /// </summary>
    [JsonPropertyName("arguments")]
    public List<CommandArgument> Arguments { get; set; } = new();

    /// <summary>
    /// Named flags
    /// </summary>
    [JsonPropertyName("flags")]
    public List<CommandFlag> Flags { get; set; } = new();

    /// <summary>
    /// Aliases
    /// </summary>
    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// Related commands
    /// </summary>
    [JsonPropertyName("relatedCommands")]
    public List<string> RelatedCommands { get; set; } = new();

    /// <summary>
    /// Tips and notes
    /// </summary>
    [JsonPropertyName("tips")]
    public List<string> Tips { get; set; } = new();

    /// <summary>
    /// Minimum privilege required
    /// </summary>
    [JsonPropertyName("privilege")]
    public Security.PrivilegeLevel RequiredPrivilege { get; set; } = Security.PrivilegeLevel.Standard;

    /// <summary>
    /// Generates Markdown documentation
    /// </summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# /{Name}");
        sb.AppendLine();
        sb.AppendLine(Description);
        sb.AppendLine();

        sb.AppendLine("**Category:** " + Category);
        if (Aliases.Count > 0)
        {
            sb.AppendLine($"**Aliases:** {string.Join(", ", Aliases.Select(a => $"/{a}"))}");
        }
        sb.AppendLine();

        sb.AppendLine("## Syntax");
        sb.AppendLine("```");
        sb.AppendLine(Syntax);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Examples");
        foreach (var example in Examples)
        {
            sb.AppendLine($"- `{example}`");
        }
        sb.AppendLine();

        if (Arguments.Count > 0)
        {
            sb.AppendLine("## Arguments");
            foreach (var arg in Arguments)
            {
                var required = arg.Required ? " (required)" : "";
                sb.AppendLine($"- `{arg.Name}`{required}: {arg.Description}");
            }
            sb.AppendLine();
        }

        if (Flags.Count > 0)
        {
            sb.AppendLine("## Flags");
            foreach (var flag in Flags)
            {
                var value = flag.RequiresValue ? " <value>" : "";
                sb.AppendLine($"- `--{flag.Name}`{value}: {flag.Description}");
            }
            sb.AppendLine();
        }

        if (Tips.Count > 0)
        {
            sb.AppendLine("## Tips");
            foreach (var tip in Tips)
            {
                sb.AppendLine($"- {tip}");
            }
            sb.AppendLine();
        }

        if (RelatedCommands.Count > 0)
        {
            sb.AppendLine("## See Also");
            foreach (var cmd in RelatedCommands)
            {
                sb.AppendLine($"- `/{cmd}`");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Command argument definition
/// </summary>
public class CommandArgument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Command flag definition
/// </summary>
public class CommandFlag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("shortForm")]
    public string? ShortForm { get; set; }

    [JsonPropertyName("requiresValue")]
    public bool RequiresValue { get; set; }
}
