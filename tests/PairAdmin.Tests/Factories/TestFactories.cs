namespace PairAdmin.Tests.Factories;

/// <summary>
/// Factory for creating test chat messages
/// </summary>
public static class ChatMessageFactory
{
    public static ChatMessage Create(
        string role = "user",
        string content = "Test message",
        DateTime? timestamp = null)
    {
        return new ChatMessage
        {
            Role = role,
            Content = content,
            Timestamp = timestamp ?? DateTime.UtcNow
        };
    }

    public static ChatMessage System(string content)
    {
        return Create("system", content);
    }

    public static ChatMessage User(string content)
    {
        return Create("user", content);
    }

    public static ChatMessage Assistant(string content)
    {
        return Create("assistant", content);
    }

    public static List<ChatMessage> CreateConversation(int messageCount = 5)
    {
        var messages = new List<ChatMessage>();
        var roles = new[] { "user", "assistant" };

        for (int i = 0; i < messageCount; i++)
        {
            messages.Add(Create(
                role: roles[i % 2],
                content: $"Message {i + 1} content"
            ));
        }

        return messages;
    }

    public static List<ChatMessage> CreateConversation(params (string role, string content)[] messages)
    {
        return messages.Select(m => Create(m.role, m.content)).ToList();
    }
}

/// <summary>
/// Factory for creating test user settings
/// </summary>
public static class UserSettingsFactory
{
    public static UserSettings Create(
        string? themeMode = null,
        int? maxLines = null,
        string? activeProvider = null)
    {
        return new UserSettings
        {
            Theme = new ThemeSettings
            {
                Mode = Enum.Parse<ThemeMode>(themeMode ?? "System"),
                AccentColor = "#3498DB",
                FontSize = 14,
                FontFamily = "Segoe UI"
            },
            ContextWindow = new ContextWindowSettings
            {
                SizeMode = ContextSizeMode.Auto,
                MaxLines = maxLines ?? 500,
                MaxTokens = 128000
            },
            ActiveProvider = activeProvider ?? "openai",
            Application = new ApplicationSettings
            {
                WindowWidth = 1200,
                WindowHeight = 800
            }
        };
    }

    public static UserSettings CreateMinimal()
    {
        return new UserSettings();
    }
}

/// <summary>
/// Factory for creating test commands
/// </summary>
public static class ParsedCommandFactory
{
    public static ParsedCommand Create(
        string commandName = "help",
        List<string>? arguments = null,
        Dictionary<string, string>? flags = null)
    {
        return new ParsedCommand
        {
            CommandName = commandName,
            Arguments = arguments ?? new List<string>(),
            Flags = flags ?? new Dictionary<string, string>(),
            OriginalText = $"/{commandName}",
            IsValid = true
        };
    }

    public static ParsedCommand CreateHelp()
    {
        return Create("help");
    }

    public static ParsedCommand CreateHelp(string argument)
    {
        return Create("help", new List<string> { argument });
    }

    public static ParsedCommand CreateClear()
    {
        return Create("clear");
    }

    public static ParsedCommand CreateContext()
    {
        return Create("context");
    }

    public static ParsedCommand CreateContext(string argument)
    {
        return Create("context", new List<string> { argument });
    }

    public static ParsedCommand CreateModel()
    {
        return Create("model");
    }

    public static ParsedCommand CreateModel(string model)
    {
        return Create("model", new List<string> { model });
    }

    public static ParsedCommand CreateStatus()
    {
        return Create("status");
    }
}

/// <summary>
/// Factory for creating test sensitive data filter patterns
/// </summary>
public static class FilterPatternFactory
{
    public static RegexPattern CreateAwsKeyPattern()
    {
        return new RegexPattern(
            "AWS Access Key",
            @"AKIA[0-9A-Z]{16}",
            preserveLength: 4);
    }

    public static RegexPattern CreateOpenAiKeyPattern()
    {
        return new RegexPattern(
            "OpenAI API Key",
            @"sk-[a-zA-Z0-9]{48}",
            preserveLength: 7);
    }

    public static RegexPattern CreateEmailPattern()
    {
        return new RegexPattern(
            "Email Address",
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            preserveLength: 0);
    }

    public static SensitiveDataFilter CreateFilterWithDefaults()
    {
        var filter = new SensitiveDataFilter();
        filter.AddPattern(CreateAwsKeyPattern());
        filter.AddPattern(CreateOpenAiKeyPattern());
        filter.AddPattern(CreateEmailPattern());
        return filter;
    }
}
