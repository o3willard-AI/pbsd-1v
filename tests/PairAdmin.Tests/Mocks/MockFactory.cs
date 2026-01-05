using Microsoft.Extensions.Logging;

namespace PairAdmin.Tests.Mocks;

/// <summary>
/// Mock factory for common test doubles
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Creates a mock LLM provider
    /// </summary>
    public static Mock<ILLMProvider> CreateMockLLMProvider(
        string model = "gpt-3.5-turbo",
        bool isConfigured = true)
    {
        var mock = new Mock<ILLMProvider>();

        mock.Setup(p => p.GetProviderInfo())
            .Returns(new Provider
            {
                Id = "test",
                Name = "Test Provider",
                Description = "Test provider for unit tests",
                DefaultModel = model,
                SupportedModels = new List<string> { model, "gpt-4" },
                MaxContextTokens = 16384,
                SupportsStreaming = true
            });

        mock.Setup(p => p.IsConfigured).Returns(isConfigured);

        mock.Setup(p => p.GetSupportedModels())
            .Returns(new List<string> { model, "gpt-4" });

        mock.Setup(p => p.GetDefaultModel()).Returns(model);

        mock.Setup(p => p.SupportsStreaming()).Returns(true);

        mock.Setup(p => p.GetMaxContext()).Returns(16384);

        mock.Setup(p => p.EstimateTokenCount(It.IsAny<string>()))
            .Returns((string text) => text.Length / 4);

        mock.Setup(p => p.ValidateConfiguration(It.IsAny<ProviderConfiguration>(), out It.Ref<string?>.IsAny))
            .Returns(true);

        return mock;
    }

    /// <summary>
    /// Creates a mock command handler
    /// </summary>
    public static Mock<ICommandHandler> CreateMockCommandHandler(
        string commandName = "test",
        CommandResult? result = null,
        bool canExecute = true)
    {
        var mock = new Mock<ICommandHandler>();

        mock.Setup(h => h.Metadata)
            .Returns(new CommandMetadata
            {
                Name = commandName,
                Description = $"Test command {commandName}",
                Category = "Test",
                Syntax = $"/{commandName}",
                Examples = new List<string> { $"/{commandName}" },
                IsAvailable = true
            });

        mock.Setup(h => h.Execute(It.IsAny<CommandContext>(), It.IsAny<ParsedCommand>()))
            .Returns(result ?? CommandResult.Success("Test result"));

        mock.Setup(h => h.GetHelpText())
            .Returns($"## /{commandName}\n\nTest command help for {commandName}");

        mock.Setup(h => h.CanExecute(It.IsAny<CommandContext>()))
            .Returns(canExecute);

        return mock;
    }

    /// <summary>
    /// Creates a mock logger
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates a mock context provider
    /// </summary>
    public static Mock<IContextProvider> CreateMockContextProvider()
    {
        var mock = new Mock<IContextProvider>();

        mock.Setup(p => p.GetContext(It.IsAny<int>()))
            .Returns((int maxTokens) => "Test context content");

        return mock;
    }

    /// <summary>
    /// Creates a mock LLM gateway
    /// </summary>
    public static LLMGateway CreateMockGateway(
        Mock<ILLMProvider>? provider = null,
        ILogger<LLMGateway>? logger = null)
    {
        provider ??= CreateMockLLMProvider();
        logger ??= CreateMockLogger<LLMGateway>().Object;

        var gateway = new LLMGateway(logger);
        gateway.RegisterProvider(provider.Object);
        gateway.SetActiveProvider("test");

        return gateway;
    }
}

/// <summary>
/// In-memory test context for tests
/// </summary>
public class TestContext
{
    public List<ChatMessage> Messages { get; } = new();
    public UserSettings Settings { get; } = new UserSettings();
    public Mock<ILLMProvider> LLMProvider { get; }
    public ContextWindowManager? ContextManager { get; set; }
    public ChatPane? ChatPane { get; set; }
    public CancellationToken CancellationToken { get; } = CancellationToken.None;

    public TestContext()
    {
        LLMProvider = MockFactory.CreateMockLLMProvider();
    }

    public CommandContext ToCommandContext()
    {
        return new CommandContext
        {
            Messages = Messages,
            Settings = Settings,
            LLMGateway = CreateTestGateway(),
            ContextManager = ContextManager,
            ChatPane = ChatPane,
            CancellationToken = CancellationToken
        };
    }

    public ParsedCommand CreateCommand(string commandText)
    {
        var parser = new SlashCommandParser();
        return parser.Parse(commandText);
    }

    private LLMGateway CreateTestGateway()
    {
        var logger = Mock.Create<ILogger<LLMGateway>>();
        var gateway = new LLMGateway(logger.Object);
        gateway.RegisterProvider(LLMProvider.Object);
        return gateway;
    }
}
