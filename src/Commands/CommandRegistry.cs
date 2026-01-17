using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PairAdmin.Commands;

/// <summary>
/// Command registry for managing available commands
/// </summary>
public class CommandRegistry
{
    private readonly ConcurrentDictionary<string, CommandEntry> _commands;
    private readonly ConcurrentDictionary<string, string> _aliases;
    private readonly ILogger<CommandRegistry> _logger;

    private class CommandEntry
    {
        public ICommandHandler Handler { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>
    /// Gets all registered command metadata
    /// </summary>
    public IReadOnlyDictionary<string, CommandMetadata> RegisteredCommands =>
        new ConcurrentDictionary<string, CommandMetadata>(
            _commands.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Handler.Metadata));

    /// <summary>
    /// Gets command count
    /// </summary>
    public int CommandCount => _commands.Count;

    /// <summary>
    /// Initializes a new instance of CommandRegistry
    /// </summary>
    public CommandRegistry(ILogger<CommandRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commands = new ConcurrentDictionary<string, CommandEntry>();
        _aliases = new ConcurrentDictionary<string, string>();

        _logger.LogInformation("CommandRegistry initialized");
    }

    /// <summary>
    /// Registers a command handler
    /// </summary>
    public void Register(ICommandHandler handler)
    {
        var metadata = handler.Metadata;
        var commandName = metadata.Name.ToLowerInvariant();

        var entry = new CommandEntry
        {
            Handler = handler,
            RegisteredAt = DateTime.Now
        };

        if (!_commands.TryAdd(commandName, entry))
        {
            _logger.LogWarning($"Command '{commandName}' already registered, replacing");
            _commands[commandName] = entry;
        }

        foreach (var alias in metadata.Aliases)
        {
            var aliasLower = alias.ToLowerInvariant();
            if (_aliases.TryAdd(aliasLower, commandName))
            {
                _logger.LogDebug($"Registered alias /{alias} for /{commandName}");
            }
        }

        _logger.LogInformation($"Registered command: /{commandName} with {metadata.Aliases.Count} aliases");
    }

    /// <summary>
    /// Unregisters a command
    /// </summary>
    public bool Unregister(string commandName)
    {
        var name = commandName.ToLowerInvariant();

        if (_commands.TryRemove(name, out var entry))
        {
            foreach (var alias in entry.Handler.Metadata.Aliases)
            {
                _aliases.TryRemove(alias.ToLowerInvariant(), out _);
            }

            _logger.LogInformation($"Unregistered command: /{name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a command handler by name
    /// </summary>
    public ICommandHandler? GetHandler(string commandName)
    {
        var name = commandName.ToLowerInvariant();

        if (_aliases.TryGetValue(name, out var realName))
        {
            name = realName;
        }

        return _commands.TryGetValue(name, out var entry) ? entry.Handler : null;
    }

    /// <summary>
    /// Gets command metadata by name
    /// </summary>
    public CommandMetadata? GetMetadata(string commandName)
    {
        return GetHandler(commandName)?.Metadata;
    }

    /// <summary>
    /// Checks if a command exists
    /// </summary>
    public bool HasCommand(string commandName)
    {
        var name = commandName.ToLowerInvariant();

        if (_commands.ContainsKey(name))
        {
            return true;
        }

        return _aliases.ContainsKey(name);
    }

    /// <summary>
    /// Gets all commands in a category
    /// </summary>
    public System.Collections.Generic.List<CommandMetadata> GetByCategory(string category)
    {
        return _commands.Values
            .Where(e => e.Handler.Metadata.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Handler.Metadata)
            .ToList();
    }

    /// <summary>
    /// Gets all available categories
    /// </summary>
    public System.Collections.Generic.List<string> GetCategories()
    {
        return _commands.Values
            .Select(e => e.Handler.Metadata.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    /// <summary>
    /// Searches for commands matching a query
    /// </summary>
    public System.Collections.Generic.List<CommandMetadata> Search(string query)
    {
        var queryLower = query.ToLowerInvariant();

        return _commands.Values
            .Where(e =>
                e.Handler.Metadata.Name.Contains(queryLower) ||
                e.Handler.Metadata.Description.Contains(queryLower) ||
                e.Handler.Metadata.Category.Contains(queryLower) ||
                e.Handler.Metadata.Aliases.Any(a => a.Contains(queryLower)))
            .Select(e => e.Handler.Metadata)
            .ToList();
    }

    /// <summary>
    /// Gets all commands as a help list
    /// </summary>
    public string GetHelpList()
    {
        var categories = GetCategories();
        var sb = new StringBuilder();

        sb.AppendLine("## Available Commands");
        sb.AppendLine();

        foreach (var category in categories)
        {
            var commands = GetByCategory(category);

            sb.AppendLine($"### {category}");
            sb.AppendLine();

            foreach (var command in commands)
            {
                sb.AppendLine($"- `/{command.Name}` - {command.Description}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("Use `/help <command>` for detailed information about a specific command.");

        return sb.ToString();
    }

    /// <summary>
    /// Clears all registered commands
    /// </summary>
    public void Clear()
    {
        var count = _commands.Count;
        _commands.Clear();
        _aliases.Clear();

        _logger.LogInformation($"Cleared {count} registered commands");
    }
}
