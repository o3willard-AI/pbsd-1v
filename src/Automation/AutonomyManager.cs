using System;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Automation;

/// <summary>
/// Autonomy modes for AI command execution
/// </summary>
public enum AutonomyMode
{
    /// <summary>AI suggests commands, user executes manually</summary>
    Manual,
    /// <summary>AI suggests commands, user confirms before execution</summary>
    Confirm,
    /// <summary>AI executes commands automatically</summary>
    Auto
}

/// <summary>
/// Event args for command suggestion events
/// </summary>
public class CommandSuggestionEventArgs : EventArgs
{
    public string Command { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public bool RequiresConfirmation { get; init; }
}

/// <summary>
/// Manages AI autonomy modes and command execution
/// </summary>
public class AutonomyManager
{
    private readonly ILogger<AutonomyManager> _logger;
    private AutonomyMode _currentMode = AutonomyMode.Manual;
    private Action<string>? _commandExecutor;

    /// <summary>
    /// Event raised when a command is suggested
    /// </summary>
    public event EventHandler<CommandSuggestionEventArgs>? CommandSuggested;

    /// <summary>
    /// Event raised when mode changes
    /// </summary>
    public event EventHandler<AutonomyMode>? ModeChanged;

    /// <summary>
    /// Current autonomy mode
    /// </summary>
    public AutonomyMode CurrentMode => _currentMode;

    public AutonomyManager(ILogger<AutonomyManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Set the command executor function (typically TerminalPane.SendCommand)
    /// </summary>
    public void SetCommandExecutor(Action<string> executor)
    {
        _commandExecutor = executor;
    }

    /// <summary>
    /// Set the autonomy mode
    /// </summary>
    public void SetMode(AutonomyMode mode)
    {
        if (_currentMode == mode) return;
        _logger.LogInformation("Autonomy mode changed from {Old} to {New}", _currentMode, mode);
        _currentMode = mode;
        ModeChanged?.Invoke(this, mode);
    }

    /// <summary>
    /// Suggest a command based on AI analysis
    /// </summary>
    public void SuggestCommand(string command, string explanation = "")
    {
        if (string.IsNullOrEmpty(command)) return;

        _logger.LogInformation("Command suggested: {Command}", command);

        var args = new CommandSuggestionEventArgs
        {
            Command = command,
            Explanation = explanation,
            RequiresConfirmation = _currentMode == AutonomyMode.Confirm
        };

        CommandSuggested?.Invoke(this, args);

        if (_currentMode == AutonomyMode.Auto)
        {
            ExecuteCommand(command);
        }
    }

    /// <summary>
    /// Execute a command through the terminal
    /// </summary>
    public bool ExecuteCommand(string command)
    {
        if (_commandExecutor == null)
        {
            _logger.LogWarning("No command executor configured");
            return false;
        }

        if (_currentMode == AutonomyMode.Manual)
        {
            _logger.LogWarning("Cannot execute in manual mode");
            return false;
        }

        _logger.LogInformation("Executing command: {Command}", command);
        _commandExecutor(command);
        return true;
    }

    /// <summary>
    /// Confirm and execute a previously suggested command
    /// </summary>
    public bool ConfirmAndExecute(string command)
    {
        if (_currentMode != AutonomyMode.Confirm)
        {
            _logger.LogWarning("ConfirmAndExecute only valid in Confirm mode");
            return false;
        }

        return ExecuteCommand(command);
    }
}
