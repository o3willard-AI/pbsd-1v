using PairAdmin.Commands;

namespace PairAdmin.Tests.Unit.Commands;

/// <summary>
/// Unit tests for command handlers
/// </summary>
public class HelpCommandHandlerTests
{
    private readonly HelpCommandHandler _handler;
    private readonly CommandRegistry _registry;
    private readonly TestContext _context;

    public HelpCommandHandlerTests()
    {
        var logger = new TestLogger<HelpCommandHandler>();
        _registry = new CommandRegistry(logger);
        _handler = new HelpCommandHandler(_registry, logger);
        _context = new TestContext();
    }

    [Fact]
    public void Execute_WithNoArguments_ReturnsCommandList()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateHelp();

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CancelSend.Should().BeTrue();
        result.Response.Should().Contain("Available Commands");
    }

    [Fact]
    public void Execute_WithValidCommand_ReturnsCommandHelp()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateHelp("context");

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("/context");
    }

    [Fact]
    public void Execute_WithUnknownCommand_ReturnsNotFound()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("help", new List<string> { "unknowncommand" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CommandStatus.NotFound);
    }

    [Fact]
    public void Metadata_HasCorrectValues()
    {
        // Assert
        _handler.Metadata.Name.Should().Be("help");
        _handler.Metadata.Category.Should().Be("Core");
        _handler.Metadata.Aliases.Should().Contain("h");
    }
}

/// <summary>
/// Unit tests for ClearCommandHandler
/// </summary>
public class ClearCommandHandlerTests
{
    private readonly ClearCommandHandler _handler;
    private readonly TestContext _context;

    public ClearCommandHandlerTests()
    {
        var logger = new TestLogger<ClearCommandHandler>();
        _handler = new ClearCommandHandler(logger);
        _context = new TestContext();

        // Add some test messages
        _context.Messages.Add(ChatMessageFactory.User("Test 1"));
        _context.Messages.Add(ChatMessageFactory.Assistant("Response 1"));
    }

    [Fact]
    public void Execute_WithNoArguments_ClearsAll()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateClear();
        _context.ContextManager = new ContextWindowManager(new TestLogger<ContextWindowManager>());

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Cleared");
    }

    [Fact]
    public void Execute_ClearMessages_ClearsOnlyMessages()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("clear", new List<string> { "messages" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Execute_WithInvalidScope_ReturnsInvalidArguments()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("clear", new List<string> { "invalid" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CommandStatus.InvalidArguments);
    }

    [Fact]
    public void CanExecute_WithMessages_ReturnsTrue()
    {
        // Act
        var result = _handler.CanExecute(_context.ToCommandContext());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithNoMessages_ReturnsFalse()
    {
        // Arrange
        var emptyContext = new TestContext();

        // Act
        var result = _handler.CanExecute(emptyContext.ToCommandContext());

        // Assert
        result.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for ContextCommandHandler
/// </summary>
public class ContextCommandHandlerTests
{
    private readonly ContextCommandHandler _handler;
    private readonly TestContext _context;

    public ContextCommandHandlerTests()
    {
        var logger = new TestLogger<ContextCommandHandler>();
        _handler = new ContextCommandHandler(logger);
        _context = new TestContext();
        _context.ContextManager = new ContextWindowManager(new TestLogger<ContextWindowManager>());
    }

    [Fact]
    public void Execute_WithNoArguments_ShowsCurrentContext()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateContext();

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Context Window Settings");
    }

    [Fact]
    public void Execute_WithLines_SetsContextLines()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateContext("100");

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.ContextManager!.MaxLines.Should().Be(100);
    }

    [Fact]
    public void Execute_WithPercentage_SetsPercentage()
    {
        // Arrange
        var command = ParsedCommandFactory.CreateContext("50%");

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.ContextManager!.Policy.Should().Be(ContextSizePolicy.Percentage);
    }

    [Fact]
    public void Execute_WithAuto_SetsAutoMode()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("context", new List<string>(), new Dictionary<string, string> { ["auto"] = "true" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.ContextManager!.Policy.Should().Be(ContextSizePolicy.Auto);
    }

    [Fact]
    public void CanExecute_WithoutContextManager_ReturnsFalse()
    {
        // Arrange
        var emptyContext = new TestContext();

        // Act
        var result = _handler.CanExecute(emptyContext.ToCommandContext());

        // Assert
        result.Should().BeFalse();
    }
}
