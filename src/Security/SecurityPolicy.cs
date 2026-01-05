namespace PairAdmin.Security;

/// <summary>
/// Security policy for command validation
/// </summary>
public class SecurityPolicy
{
    /// <summary>
    /// Validation mode
    /// </summary>
    public ValidationMode Mode { get; set; } = ValidationMode.Blacklist;

    /// <summary>
    /// Default privilege level for new sessions
    /// </summary>
    public PrivilegeLevel DefaultPrivilegeLevel { get; set; } = PrivilegeLevel.Standard;

    /// <summary>
    /// Whitelisted commands (only used in whitelist mode)
    /// </summary>
    public HashSet<string> WhitelistedCommands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Blacklisted commands (used in blacklist mode)
    /// </summary>
    public HashSet<string> BlacklistedCommands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Commands requiring elevated privileges
    /// </summary>
    public Dictionary<string, PrivilegeLevel> CommandPrivilegeRequirements { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Enable detection of dangerous command patterns
    /// </summary>
    public bool EnableDangerousCommandDetection { get; set; } = true;

    /// <summary>
    /// Allowed command categories (empty means all allowed)
    /// </summary>
    public HashSet<string> AllowedCategories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maximum command length
    /// </summary>
    public int MaxCommandLength { get; set; } = 1000;

    /// <summary>
    /// Enable audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Commands that require confirmation
    /// </summary>
    public HashSet<string> CommandsRequiringConfirmation { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a default security policy
    /// </summary>
    public static SecurityPolicy CreateDefault()
    {
        return new SecurityPolicy
        {
            Mode = ValidationMode.Blacklist,
            DefaultPrivilegeLevel = PrivilegeLevel.Standard,
            EnableDangerousCommandDetection = true,
            MaxCommandLength = 1000,
            EnableAuditLogging = true
        };
    }

    /// <summary>
    /// Creates a strict security policy
    /// </summary>
    public static SecurityPolicy CreateStrict()
    {
        var policy = CreateDefault();
        policy.Mode = ValidationMode.Whitelist;
        policy.WhitelistedCommands.Add("help");
        policy.WhitelistedCommands.Add("status");
        policy.WhitelistedCommands.Add("context");
        policy.EnableDangerousCommandDetection = true;
        return policy;
    }
}

/// <summary>
/// Validation mode
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Only allow commands in the whitelist
    /// </summary>
    Whitelist,

    /// <summary>
    /// Block commands in the blacklist
    /// </summary>
    Blacklist,

    /// <summary>
    /// No validation, allow all commands
    /// </summary>
    Disabled
}

/// <summary>
/// Privilege level for command execution
/// </summary>
public enum PrivilegeLevel
{
    /// <summary>
    /// Standard user privileges
    /// </summary>
    Standard,

    /// <summary>
    /// Elevated/sudo privileges
    /// </summary>
    Elevated,

    /// <summary>
    /// Restricted mode with limited commands
    /// </summary>
    Restricted
}
