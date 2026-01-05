using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Security;

/// <summary>
/// Validates commands for dangerous patterns
/// </summary>
public class DangerousCommandValidator : CommandValidatorBase
{
    private static readonly Regex[] DangerousPatterns =
    {
        new Regex(@"[;&|`$]\s*\w", RegexOptions.Compiled),  // Command chaining
        new Regex(@"\.\.\/", RegexOptions.Compiled),         // Path traversal
        new Regex(@"\|\s*\(|\)\s*\|", RegexOptions.Compiled), // Pipeline injection
        new Regex(@"\$\{?\w+\}?", RegexOptions.Compiled),    // Variable expansion
        new Regex(@"0x[0-9a-fA-F]+", RegexOptions.Compiled), // Hex encoding
        new Regex(@"\\x[0-9a-fA-F]{2}", RegexOptions.Compiled), // Escaped hex
        new Regex(@"%[0-9a-fA-F]{2}", RegexOptions.Compiled), // URL encoding
    };

    private static readonly HashSet<string> DangerousCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm", "format", "mkfs", "dd", "shred", "kill", "pkill", "killall",
        "chmod", "chown", "setenforce", "iptables", "ufw",
        "systemctl", "service", "init", "shutdown", "reboot", "halt",
        "wget", "curl", "nc", "netcat", "ssh", "scp"
    };

    public override string Name => "DangerousCommandValidator";

    public DangerousCommandValidator(SecurityPolicy policy, ILogger? logger = null)
        : base(policy, logger) { }

    public override CommandValidationResult Validate(
        string command,
        Commands.CommandContext context,
        PrivilegeLevel privilegeLevel)
    {
        if (!_policy.EnableDangerousCommandDetection)
        {
            return CommandValidationResult.Success();
        }

        var commandName = ExtractCommandName(command);

        if (DangerousCommands.Contains(commandName))
        {
            var warnings = new List<string>
            {
                $"Command '/{commandName}' is classified as potentially dangerous",
                "Ensure you have verified the command before proceeding"
            };

            _logger?.LogWarning("Dangerous command detected: {Command}", commandName);
            return CommandValidationResult.Warning(
                $"Potentially dangerous command: /{commandName}",
                warnings);
        }

        foreach (var pattern in DangerousPatterns)
        {
            if (pattern.IsMatch(command))
            {
                _logger?.LogWarning("Dangerous pattern detected in command: {Pattern}", pattern);
                return CommandValidationResult.Forbidden(
                    "Command contains potentially dangerous patterns");
            }
        }

        return CommandValidationResult.Success();
    }
}
