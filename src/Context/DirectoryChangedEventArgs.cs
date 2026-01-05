using System;

namespace PairAdmin.Context;

/// <summary>
/// Type of directory change
/// </summary>
public enum DirectoryChangeType
{
    UserCommand,    // User typed command (cd)
    ParsedPrompt,  // Detected from prompt
    StackOperation  // push/pop from history
}

/// <summary>
/// Event arguments for directory change events
/// </summary>
public class DirectoryChangedEventArgs : EventArgs
{
    /// <summary>
    /// Old directory path
    /// </summary>
    public string OldDirectory { get; set; }

    /// <summary>
    /// New directory path
    /// </summary>
    public string NewDirectory { get; set; }

    /// <summary>
    /// Type of change
    /// </summary>
    public DirectoryChangeType ChangeType { get; set; }

    /// <summary>
    /// Timestamp of change
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Username who triggered change (if available)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Hostname (if available)
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Additional data about the change
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Gets the relative path from old to new
    /// </summary>
    /// <returns>Relative path or null</returns>
    public string? GetRelativePath()
    {
        if (string.IsNullOrWhiteSpace(OldDirectory) || string.IsNullOrWhiteSpace(NewDirectory))
        {
            return null;
        }

        try
        {
            var relative = Path.GetRelativePath(OldDirectory, NewDirectory);
            return relative == "." ? string.Empty : relative;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets whether this is a parent directory change
    /// </summary>
    /// <returns>True if new directory is a subdirectory of old</returns>
    public bool IsParentDirectoryChange()
    {
        if (string.IsNullOrWhiteSpace(OldDirectory) || string.IsNullOrWhiteSpace(NewDirectory))
        {
            return false;
        }

        return NewDirectory.StartsWith(OldDirectory + Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Gets whether this is a back directory change (cd ..)
    /// </summary>
    /// <returns>True if going back in directory tree</returns>
    public bool IsBackDirectoryChange()
    {
        if (string.IsNullOrWhiteSpace(OldDirectory) || string.IsNullOrWhiteSpace(NewDirectory))
        {
            return false;
        }

        var oldParts = OldDirectory.Split(Path.DirectorySeparatorChar);
        var newParts = NewDirectory.Split(Path.DirectorySeparatorChar);

        if (oldParts.Length > 0 && newParts.Length > 0)
        {
            return newParts.Length < oldParts.Length;
        }

        return false;
    }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public DirectoryChangedEventArgs()
    {
        Timestamp = DateTime.Now;
        OldDirectory = string.Empty;
        NewDirectory = string.Empty;
        ChangeType = DirectoryChangeType.UserCommand;
    }

    /// <summary>
    /// Initializes a new instance with values
    /// </summary>
    public DirectoryChangedEventArgs(
        string oldDirectory,
        string newDirectory,
        DirectoryChangeType changeType)
    {
        Timestamp = DateTime.Now;
        OldDirectory = oldDirectory;
        NewDirectory = newDirectory;
        ChangeType = changeType;
    }

    /// <summary>
    /// Gets a summary of the change
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetSummary()
    {
        return ChangeType switch
        {
            DirectoryChangeType.UserCommand => $"Changed to {NewDirectory}",
            DirectoryChangeType.ParsedPrompt => $"Detected: {NewDirectory}",
            DirectoryChangeType.StackOperation => $"{ChangeType}: {NewDirectory}",
            _ => $"Changed to {NewDirectory}"
        };
    }

    /// <summary>
    /// Creates a copy of this event args
    /// </summary>
    /// <returns>Cloned DirectoryChangedEventArgs</returns>
    public DirectoryChangedEventArgs Clone()
    {
        return new DirectoryChangedEventArgs
        {
            OldDirectory = OldDirectory,
            NewDirectory = NewDirectory,
            ChangeType = ChangeType,
            Timestamp = Timestamp,
            Username = Username,
            Hostname = Hostname,
            AdditionalData = AdditionalData != null
                ? new Dictionary<string, object>(AdditionalData)
                : null
        };
    }
}
