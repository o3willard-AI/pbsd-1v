namespace PairAdmin.LLMGateway.Models;

/// <summary>
/// Message role for LLM API
/// </summary>
public enum MessageRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// Message model for LLM API requests
/// </summary>
public class Message
{
    /// <summary>
    /// Message role (system, user, assistant)
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Optional timestamp
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets the role as a string (for JSON serialization)
    /// </summary>
    public string RoleString => Role switch
    {
        MessageRole.System => "system",
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "user"
    };

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public Message()
    {
        Role = MessageRole.User;
        Content = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance with role and content
    /// </summary>
    public Message(MessageRole role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Creates a system message
    /// </summary>
    public static Message SystemMessage(string content)
    {
        return new Message(MessageRole.System, content);
    }

    /// <summary>
    /// Creates a user message
    /// </summary>
    public static Message UserMessage(string content)
    {
        return new Message(MessageRole.User, content);
    }

    /// <summary>
    /// Creates an assistant message
    /// </summary>
    public static Message AssistantMessage(string content)
    {
        return new Message(MessageRole.Assistant, content);
    }

    /// <summary>
    /// Gets a clone of this message
    /// </summary>
    public Message Clone()
    {
        return new Message
        {
            Role = Role,
            Content = Content,
            Timestamp = Timestamp
        };
    }

    /// <summary>
    /// Converts message to ChatMessage format
    /// </summary>
    public Chat.ChatMessage ToChatMessage()
    {
        var sender = Role switch
        {
            MessageRole.User => Chat.MessageSender.User,
            MessageRole.Assistant => Chat.MessageSender.Assistant,
            MessageRole.System => Chat.MessageSender.System,
            _ => Chat.MessageSender.System
        };

        return new Chat.ChatMessage
        {
            Sender = sender,
            Content = Content,
            Timestamp = Timestamp ?? DateTime.Now
        };
    }
}
