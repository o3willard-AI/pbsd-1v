namespace PairAdmin.Chat;

/// <summary>
/// Message sender type
/// </summary>
public enum MessageSender
{
    User,
    Assistant,
    System
}

/// <summary>
/// Command block information for code snippets in chat
/// </summary>
public class CommandBlock
{
    /// <summary>
    /// Language of the code (e.g., bash, python)
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Code content
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Start line number in the original message
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// End line number in the original message
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Unique identifier for the command block
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the number of lines in the code block
    /// </summary>
    public int LineCount => Code.Split('\n').Length;
}

/// <summary>
/// Chat message model
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Message sender (User, Assistant, System)
    /// </summary>
    public MessageSender Sender { get; set; }

    /// <summary>
    /// Message content (supports Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when message was sent
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Token count of the message
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Command blocks detected in the message
    /// </summary>
    public List<CommandBlock> CommandBlocks { get; set; } = new List<CommandBlock>();

    /// <summary>
    /// Whether the message is currently being streamed
    /// </summary>
    public bool IsStreaming { get; set; }

    /// <summary>
    /// Whether the message is a command response
    /// </summary>
    public bool IsCommandResponse { get; set; }

    /// <summary>
    /// Parent message ID (for threading)
    /// </summary>
    public string? ParentMessageId { get; set; }

    /// <summary>
    /// Gets formatted timestamp string
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("HH:mm");

    /// <summary>
    /// Gets sender display name
    /// </summary>
    public string SenderDisplayName => Sender switch
    {
        MessageSender.User => "You",
        MessageSender.Assistant => "AI Assistant",
        MessageSender.System => "System",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets whether the message has any command blocks
    /// </summary>
    public bool HasCommandBlocks => CommandBlocks.Count > 0;

    /// <summary>
    /// Gets the number of command blocks
    /// </summary>
    public int CommandBlockCount => CommandBlocks.Count;

    /// <summary>
    /// Extracts command blocks from message content
    /// </summary>
    public void ExtractCommandBlocks()
    {
        CommandBlocks.Clear();

        var pattern = @"```(\w+)?\n([\s\S]*?)```";
        var matches = System.Text.RegularExpressions.Regex.Matches(Content, pattern);

        int lineCounter = 0;
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value;

            var commandBlock = new CommandBlock
            {
                Language = string.IsNullOrEmpty(language) ? "text" : language,
                Code = code.Trim(),
                StartLine = lineCounter,
                EndLine = lineCounter + code.Split('\n').Length
            };

            CommandBlocks.Add(commandBlock);
            lineCounter = commandBlock.EndLine;
        }
    }

    /// <summary>
    /// Updates message content during streaming
    /// </summary>
    /// <param name="additionalContent">Additional content to append</param>
    public void AppendContent(string additionalContent)
    {
        Content += additionalContent;
    }

    /// <summary>
    /// Marks message as complete (streaming finished)
    /// </summary>
    public void Complete()
    {
        IsStreaming = false;
        ExtractCommandBlocks();
    }

    /// <summary>
    /// Creates a copy of the message
    /// </summary>
    /// <returns>Cloned ChatMessage</returns>
    public ChatMessage Clone()
    {
        return new ChatMessage
        {
            Id = Id,
            Sender = Sender,
            Content = Content,
            Timestamp = Timestamp,
            TokenCount = TokenCount,
            CommandBlocks = CommandBlocks.Select(cb => new CommandBlock
            {
                Language = cb.Language,
                Code = cb.Code,
                StartLine = cb.StartLine,
                EndLine = cb.EndLine,
                Id = cb.Id
            }).ToList(),
            IsStreaming = IsStreaming,
            IsCommandResponse = IsCommandResponse,
            ParentMessageId = ParentMessageId
        };
    }
}

/// <summary>
/// Message sent event arguments
/// </summary>
public class MessageSentEventArgs : EventArgs
{
    public ChatMessage Message { get; set; }
}

/// <summary>
/// Message received event arguments
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public ChatMessage Message { get; set; }
}

/// <summary>
/// Command copied event arguments
/// </summary>
public class CommandCopiedEventArgs : EventArgs
{
    public CommandBlock CommandBlock { get; set; }
    public string CopiedText { get; set; }
}
