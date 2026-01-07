using PairAdmin.Commands;
using PairAdmin.Help;

namespace PairAdmin.Tests.Unit.Commands;

/// <summary>
/// Unit tests for CommandDispatcher
/// </summary>
public class CommandDispatcherTests
{
    private readonly CommandDispatcher _dispatcher;
    private readonly CommandRegistry _registry;
    private readonly SlashCommandParser _parser;
    private readonly TestLogger<CommandDispatcher> _logger;
    private readonly TestContext _context;

    public CommandDispatcherTests()
    {
        _logger = new TestLogger<CommandDispatcher>();
        _registry = new CommandRegistry(_logger);
        _parser = new SlashCommandParser(_logger);
        _dispatcher = new CommandDispatcher(_registry, _parser, _logger);
        _context = new TestContext();
    }

    [Fact]
    public void Execute_WithEmptyInput_ReturnsError()
    {
        // Act
        var result = _dispatcher.Execute("", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CommandStatus.Error);
        result.Response.Should().Contain("No command provided");
    }

    [Fact]
    public void Execute_WithWhitespaceInput_ReturnsError()
    {
        // Act
        var result = _dispatcher.Execute("   ", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Execute_WithNonSlashInput_ReturnsEmptySuccess()
    {
        // Act
        var result = _dispatcher.Execute("just some text", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CancelSend.Should().BeFalse();
        result.Response.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WithUnknownCommand_ReturnsNotFound()
    {
        // Act
        var result = _dispatcher.Execute("/unknowncommand", _context.ToCommandContext());

        // Assert
        result.Status.Should().Be(CommandStatus.NotFound);
        result.Response.Should().Contain("not found");
        result.Response.Should().Contain("/help");
    }

    [Fact]
    public void Execute_WithUnknownCommand_ProvidesSuggestions()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("context").Object;
        _registry.Register(handler);

        // Act
        var result = _dispatcher.Execute("/contex", _context.ToCommandContext());

        // Assert
        result.Status.Should().Be(CommandStatus.NotFound);
        result.AdditionalData.Should().ContainKey("Suggestions");
    }

    [Fact]
    public void Execute_TriggersCommandExecutedEvent()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test").Object;
        _registry.Register(handler);

        CommandResult? capturedResult = null;
        _dispatcher.CommandExecuted += (sender, result) => capturedResult = result;

        // Act
        _dispatcher.Execute("/test", _context.ToCommandContext());

        // Assert
        capturedResult.Should().NotBeNull();
        capturedResult!.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Execute_TriggersCommandNotFoundEvent()
    {
        // Arrange
        string? capturedCommand = null;
        _dispatcher.CommandNotFound += (sender, command) => capturedCommand = command;

        // Act
        _dispatcher.Execute("/nonexistent", _context.ToCommandContext());

        // Assert
        capturedCommand.Should().Be("nonexistent");
    }

    [Fact]
    public void Execute_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler(
            "test",
            CommandResult.Success("Test response")
        ).Object;
        _registry.Register(handler);

        // Act
        var result = _dispatcher.Execute("/test", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Be("Test response");
    }

    [Fact]
    public void Execute_WithCaseInsensitiveCommand_LowercasesName()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("help").Object;
        _registry.Register(handler);

        // Act
        var result = _dispatcher.Execute("/HELP", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Execute_CommandNotAvailable_ReturnsNotAvailable()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test", canExecute: false).Object;
        _registry.Register(handler);

        // Act
        var result = _dispatcher.Execute("/test", _context.ToCommandContext());

        // Assert
        result.Status.Should().Be(CommandStatus.NotAvailable);
        result.Response.Should().Contain("not available");
    }

    [Fact]
    public void Execute_WithInvalidArguments_ReturnsError()
    {
        // Arrange
        var mockHandler = MockFactory.CreateMockCommandHandler("test");
        mockHandler.Setup(h => h.CanExecute(It.IsAny<CommandContext>())).Returns(true);
        mockHandler.Setup(h => h.Metadata).Returns(new CommandMetadata
        {
            Name = "test",
            MinArguments = 1,
            MaxArguments = 1,
            Syntax = "/test <arg>"
        });
        _registry.Register(mockHandler.Object);

        // Act
        var result = _dispatcher.Execute("/test", _context.ToCommandContext());

        // Assert
        result.Status.Should().Be(CommandStatus.InvalidArguments);
        result.Response.Should().Contain("/test <arg>");
    }

    [Fact]
    public void Execute_TriggersCommandErrorEvent_OnParseError()
    {
        // Arrange
        CommandResult? capturedResult = null;
        _dispatcher.CommandError += (sender, result) => capturedResult = result;

        // Act
        _dispatcher.Execute("/   ", _context.ToCommandContext());

        // Assert
        capturedResult.Should().NotBeNull();
        capturedResult!.Status.Should().Be(CommandStatus.Error);
    }

    [Fact]
    public void Execute_HandlesException_Gracefully()
    {
        // Arrange
        var mockHandler = MockFactory.CreateMockCommandHandler("test");
        mockHandler.Setup(h => h.Execute(It.IsAny<CommandContext>(), It.IsAny<ParsedCommand>()))
            .Throws(new InvalidOperationException("Test exception"));
        _registry.Register(mockHandler.Object);

        // Act
        var result = _dispatcher.Execute("/test", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CommandStatus.Error);
        result.Response.Should().Contain("Test exception");
    }

    [Fact]
    public void GetRegistry_ReturnsRegistry()
    {
        // Act
        var registry = _dispatcher.GetRegistry();

        // Assert
        registry.Should().Be(_registry);
    }

    [Fact]
    public void GetParser_ReturnsParser()
    {
        // Act
        var parser = _dispatcher.GetParser();

        // Assert
        parser.Should().Be(_parser);
    }

    [Fact]
    public void GetSuggestions_WithPartialCommand_ReturnsMatches()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("context").Object;
        handler.Metadata.Category = "Core";
        _registry.Register(handler);

        // Act
        var suggestions = _dispatcher.GetSuggestions("/con");

        // Assert
        suggestions.Should().Contain("/context");
    }
}

/// <summary>
/// Tests for CommandDispatcher with registered handlers
/// </summary>
public class CommandDispatcherWithHandlersTests
{
    private readonly CommandDispatcher _dispatcher;
    private readonly TestContext _context;

    public CommandDispatcherWithHandlersTests()
    {
        var logger = new TestLogger<CommandDispatcher>();
        var registry = new CommandRegistry(logger);
        var parser = new SlashCommandParser(logger);
        _dispatcher = new CommandDispatcher(registry, parser, logger);
        _context = new TestContext();

        var helpHandler = new HelpCommandHandler(registry, logger);
        registry.Register(helpHandler);

        var clearHandler = new ClearCommandHandler(logger);
        registry.Register(clearHandler);

        var contextHandler = new ContextCommandHandler(logger);
        registry.Register(contextHandler);
    }

    [Fact]
    public void Execute_HelpCommand_ReturnsCommandList()
    {
        // Act
        var result = _dispatcher.Execute("/help", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CancelSend.Should().BeTrue();
        result.Response.Should().Contain("Available Commands");
    }

    [Fact]
    public void Execute_HelpWithCommandArgument_ReturnsCommandHelp()
    {
        // Act
        var result = _dispatcher.Execute("/help context", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("/context");
    }

    [Fact]
    public void Execute_ClearCommand_ClearsMessages()
    {
        // Arrange
        _context.Messages.Add(ChatMessageFactory.User("Test 1"));
        _context.Messages.Add(ChatMessageFactory.Assistant("Response 1"));

        // Act
        var result = _dispatcher.Execute("/clear messages", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Execute_ContextCommand_ShowsContextSettings()
    {
        // Act
        var result = _dispatcher.Execute("/context", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Context Window");
    }

    [Fact]
    public void Execute_ContextWithNumericArgument_SetsMaxLines()
    {
        // Arrange
        _context.ContextManager = new ContextWindowManager(new TestLogger<ContextWindowManager>());

        // Act
        var result = _dispatcher.Execute("/context 100", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.ContextManager!.MaxLines.Should().Be(100);
    }

    [Fact]
    public void Execute_WithAlias_UsesCorrectHandler()
    {
        // Act
        var result = _dispatcher.Execute("/h", _context.ToCommandContext());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Available Commands");
    }
}
