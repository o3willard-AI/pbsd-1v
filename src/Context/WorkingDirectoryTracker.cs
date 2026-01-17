using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Context;

/// <summary>
/// State for working directory tracking
/// </summary>
public class DirectoryState : INotifyPropertyChanged
{
    private string _currentDirectory;
    private string _username;
    private string _hostname;
    private Stack<string>? _history;
    private int _maxHistorySize;

    /// <summary>
    /// Current working directory
    /// </summary>
    public string CurrentDirectory
    {
        get => _currentDirectory;
        set
        {
            if (_currentDirectory != value)
            {
                _currentDirectory = value;
                OnPropertyChanged(nameof(CurrentDirectory));
                OnPropertyChanged(nameof(IsHomeDirectory));
            }
        }
    }

    /// <summary>
    /// Username from prompt
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
    }

    /// <summary>
    /// Hostname from prompt
    /// </summary>
    public string Hostname
    {
        get => _hostname;
        set
        {
            if (_hostname != value)
            {
                _hostname = value;
                OnPropertyChanged(nameof(Hostname));
            }
        }
    }

    /// <summary>
    /// Directory history stack
    /// </summary>
    public Stack<string>? History
    {
        get => _history;
        set
        {
            if (_history != value)
            {
                _history = value;
                OnPropertyChanged(nameof(History));
                OnPropertyChanged(nameof(HistoryCount));
            }
        }
    }

    /// <summary>
    /// Number of directories in history
    /// </summary>
    public int HistoryCount => _history?.Count ?? 0;

    /// <summary>
    /// Maximum history size
    /// </summary>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set
        {
            if (_maxHistorySize != value)
            {
                _maxHistorySize = value;
                OnPropertyChanged(nameof(MaxHistorySize));
            }
        }
    }

    /// <summary>
    /// Whether current directory is home directory
    /// </summary>
    public bool IsHomeDirectory
    {
        get
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return string.Equals(CurrentDirectory, homeDir, StringComparison.OrdinalIgnoreCase);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Working directory tracker with history and event system
/// </summary>
public class WorkingDirectoryTracker
{
    private readonly TerminalPromptParser _parser;
    private readonly ILogger<WorkingDirectoryTracker> _logger;
    private readonly DirectoryState _state;
    private readonly ConcurrentQueue<string> _commandQueue;

    /// <summary>
    /// Maximum history size
    /// </summary>
    private const int DefaultMaxHistory = 50;

    /// <summary>
    /// Event raised when directory changes
    /// </summary>
    public event EventHandler<DirectoryChangedEventArgs>? DirectoryChanged;

    /// <summary>
    /// Event raised when history stack changes
    /// </summary>
    public event EventHandler? HistoryStackChanged;

    /// <summary>
    /// Gets current directory state
    /// </summary>
    public DirectoryState State => _state;

    /// <summary>
    /// Gets current working directory
    /// </summary>
    public string GetCurrentDirectory()
    {
        return _state.CurrentDirectory;
    }

    /// <summary>
    /// Gets directory history
    /// </summary>
    public Stack<string>? GetHistory()
    {
        return _state.History;
    }

    /// <summary>
    /// Gets history count
    /// </summary>
    public int GetHistoryCount()
    {
        return _state.HistoryCount;
    }

    /// <summary>
    /// Initializes a new instance of WorkingDirectoryTracker
    /// </summary>
    public WorkingDirectoryTracker(
        TerminalPromptParser parser,
        ILogger<WorkingDirectoryTracker>? logger = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WorkingDirectoryTracker>.Instance;
        _state = new DirectoryState
        {
            CurrentDirectory = _parser.GetHomeDirectory(),
            History = new Stack<string>(DefaultMaxHistory),
            MaxHistorySize = DefaultMaxHistory
        };
        _commandQueue = new ConcurrentQueue<string>();

        _logger.LogInformation("WorkingDirectoryTracker initialized");
    }

    /// <summary>
    /// Parses terminal output to detect directory changes
    /// </summary>
    /// <param name="terminalOutput">Terminal output line</param>
    public void ParseTerminalOutput(string terminalOutput)
    {
        if (string.IsNullOrWhiteSpace(terminalOutput))
        {
            return;
        }

        var parsed = _parser.ParsePrompt(terminalOutput);

        if (parsed.IsDetected && !string.IsNullOrWhiteSpace(parsed.Directory))
        {
            var expandedDir = parsed.GetExpandedDirectory(_parser.GetHomeDirectory());

            if (!string.Equals(expandedDir, _state.CurrentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                SetDirectory(expandedDir, DirectoryChangeType.ParsedPrompt, parsed);
            }
        }

        _state.Username = parsed.Username ?? _state.Username;
        _state.Hostname = parsed.Hostname ?? _state.Hostname;
    }

    /// <summary>
    /// Sets the current directory
    /// </summary>
    /// <param name="directory">New directory path</param>
    /// <param name="changeType">Type of change</param>
    /// <param name="source">Source prompt or null</param>
    public void SetDirectory(
        string directory,
        DirectoryChangeType changeType = DirectoryChangeType.UserCommand,
        ParsedPrompt? source = null)
    {
        var oldDirectory = _state.CurrentDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            _logger.LogWarning("Cannot set directory to empty path");
            return;
        }

        if (!string.Equals(directory, oldDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _state.CurrentDirectory = directory;

            PushToHistory(oldDirectory);

            var args = new DirectoryChangedEventArgs(
                oldDirectory,
                directory,
                changeType)
            {
                Username = _state.Username,
                Hostname = _state.Hostname
            };

            DirectoryChanged?.Invoke(this, args);
            HistoryStackChanged?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation($"Directory changed: {oldDirectory} -> {directory}");
        }
    }

    /// <summary>
    /// Pushes directory to history stack
    /// </summary>
    /// <param name="directory">Directory to push</param>
    public void Pushd(string directory)
    {
        if (_state.History == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(directory))
        {
            _state.History.Push(directory);

            if (_state.History.Count > _state.MaxHistorySize)
            {
                _state.History.Pop();
            }

            HistoryStackChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation($"Pushd to history: {directory} (Stack: {_state.History.Count})");
        }
    }

    /// <summary>
    /// Pops directory from history stack
    /// </summary>
    /// <returns>Popped directory or null</returns>
    public string? Popd()
    {
        if (_state.History == null || _state.History.Count == 0)
        {
            _logger.LogWarning("Cannot pop from empty history");
            return null;
        }

        var popped = _state.History.Pop();
        SetDirectory(popped, DirectoryChangeType.StackOperation);

        HistoryStackChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation($"Popped from history: {popped} (Stack: {_state.History.Count})");

        return popped;
    }

    /// <summary>
    /// Rotates through directory history
    /// </summary>
    public void Dirs()
    {
        if (_state.History == null || _state.History.Count < 2)
        {
            return;
        }

        var current = _state.History.Pop();
        _state.History.Push(current);
        SetDirectory(current, DirectoryChangeType.StackOperation);

        HistoryStackChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation($"Rotated history: {current} (Stack: {_state.History.Count})");
    }

    /// <summary>
    /// Gets the formatted current directory string
    /// </summary>
    /// <returns>Formatted directory (shortened if long)</returns>
    public string GetFormattedDirectory()
    {
        var dir = _state.CurrentDirectory;

        if (dir.Length > 40)
        {
            var parts = dir.Split(Path.DirectorySeparatorChar);
            var parent = string.Join(Path.DirectorySeparatorChar, parts.Take(Math.Max(1, parts.Length - 2)));
            return Path.Combine(parent, "...");
        }

        return dir;
    }

    /// <summary>
    /// Gets the relative path to home directory
    /// </summary>
    /// <returns>Relative path or null</returns>
    public string? GetRelativeHomePath()
    {
        var homeDir = _parser.GetHomeDirectory();

        try
        {
            var relative = Path.GetRelativePath(homeDir, _state.CurrentDirectory);
            return relative == "." ? "~" : "~" + relative;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resets tracker to home directory
    /// </summary>
    public void ResetToHome()
    {
        var homeDir = _parser.GetHomeDirectory();
        SetDirectory(homeDir, DirectoryChangeType.UserCommand);

        _logger.LogInformation($"Reset to home directory: {homeDir}");
    }

    /// <summary>
    /// Clears directory history
    /// </summary>
    public void ClearHistory()
    {
        if (_state.History == null)
        {
            return;
        }

        var count = _state.History.Count;
        _state.History.Clear();

        HistoryStackChanged?.Invoke(this, EventArgs.Empty);

        _logger.LogInformation($"Cleared directory history (removed {count} entries)");
    }

    /// <summary>
    /// Gets current directory for context
    /// </summary>
    /// <returns>Formatted context string</returns>
    public string GetContextString()
    {
        return $"Working directory: {_state.CurrentDirectory}";
    }

    private void PushToHistory(string directory)
    {
        if (_state.History == null)
        {
            return;
        }

        _state.History.Push(directory);

        if (_state.History.Count > _state.MaxHistorySize)
        {
            _state.History.Pop();
        }
    }

    /// <summary>
    /// Processes queued commands
    /// </summary>
    public void ProcessCommandQueue()
    {
        while (_commandQueue.TryDequeue(out var command))
        {
            ProcessCommand(command);
        }
    }

    /// <summary>
    /// Processes a directory change command
    /// </summary>
    private void ProcessCommand(string command)
    {
        var trimmed = command.Trim();

        if (trimmed.StartsWith("cd "))
        {
            var dir = trimmed.Substring(3);
            SetDirectory(dir, DirectoryChangeType.UserCommand);
        }
        else if (trimmed == "pushd")
        {
            Pushd(directory: _state.CurrentDirectory);
        }
        else if (trimmed == "popd")
        {
            Popd();
        }
        else if (trimmed == "dirs")
        {
            Dirs();
        }
    }

    /// <summary>
    /// Queues a command for processing
    /// </summary>
    public void QueueCommand(string command)
    {
        _commandQueue.Enqueue(command);
        _logger.LogTrace($"Queued command: {command}");
    }
}
