using PairAdmin.Commands;

namespace PairAdmin.Tests.Unit.Commands;

/// <summary>
/// Unit tests for SlashCommandParser
/// </summary>
public class SlashCommandParserTests
{
    private readonly SlashCommandParser _parser;

    public SlashCommandParserTests()
    {
        _parser = new SlashCommandParser();
    }

    [Fact]
    public void Parse_WithValidCommand_ReturnsParsedCommand()
    {
        // Arrange
        var input = "/help";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CommandName.Should().Be("help");
        result.Arguments.Should().BeEmpty();
        result.OriginalText.Should().Be("/help");
    }

    [Fact]
    public void Parse_WithArguments_ReturnsParsedCommand()
    {
        // Arrange
        var input = "/help context";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CommandName.Should().Be("help");
        result.Arguments.Should().Contain("context");
    }

    [Fact]
    public void Parse_WithMultipleArguments_ReturnsAllArguments()
    {
        // Arrange
        var input = "/context 100 --percentage 50";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.CommandName.Should().Be("context");
        result.Arguments.Should().HaveCount(1);
        result.Arguments.Should().Contain("100");
        result.Flags.Should().ContainKey("percentage");
        result.Flags["percentage"].Should().Be("50");
    }

    [Fact]
    public void Parse_WithQuotedArgument_ParsesCorrectly()
    {
        // Arrange
        var input = "/filter add \"password with spaces\"";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Arguments.Should().Contain("password with spaces");
    }

    [Fact]
    public void Parse_WithSingleQuotes_ParsesCorrectly()
    {
        // Arrange
        var input = "/filter add 'test pattern'";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Arguments.Should().Contain("test pattern");
    }

    [Fact]
    public void Parse_WithoutSlash_ReturnsInvalid()
    {
        // Arrange
        var input = "help";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ParseError.Should().Contain("must start with /");
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsInvalid()
    {
        // Arrange
        var input = "";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ParseError.Should().Contain("Empty command");
    }

    [Fact]
    public void Parse_WhitespaceInput_ReturnsInvalid()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_CaseInsensitiveCommandName_LowercasesName()
    {
        // Arrange
        var input = "/HELP";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.CommandName.Should().Be("help");
    }

    [Fact]
    public void IsCommand_WithCommand_ReturnsTrue()
    {
        // Act & Assert
        _parser.IsCommand("/help").Should().BeTrue();
        _parser.IsCommand("/context 100").Should().BeTrue();
    }

    [Fact]
    public void IsCommand_WithNonCommand_ReturnsFalse()
    {
        // Act & Assert
        _parser.IsCommand("help").Should().BeFalse();
        _parser.IsCommand("").Should().BeFalse();
        _parser.IsCommand("  ").Should().BeFalse();
    }

    [Fact]
    public void ValidateArguments_WithValidCount_ReturnsTrue()
    {
        // Arrange
        var metadata = new CommandMetadata
        {
            MinArguments = 0,
            MaxArguments = 1
        };
        var command = ParsedCommandFactory.CreateHelp("context");

        // Act
        var result = _parser.ValidateArguments(command, metadata);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateArguments_WithTooFewArguments_ReturnsFalse()
    {
        // Arrange
        var metadata = new CommandMetadata
        {
            MinArguments = 1,
            MaxArguments = 2
        };
        var command = ParsedCommandFactory.CreateHelp();

        // Act
        var result = _parser.ValidateArguments(command, metadata);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateArguments_WithTooManyArguments_ReturnsFalse()
    {
        // Arrange
        var metadata = new CommandMetadata
        {
            MinArguments = 0,
            MaxArguments = 1
        };
        var command = ParsedCommandFactory.Create("help", new List<string> { "a", "b" });

        // Act
        var result = _parser.ValidateArguments(command, metadata);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSuggestions_WithPartialCommand_ReturnsMatches()
    {
        // Arrange
        var commands = new List<CommandMetadata>
        {
            new CommandMetadata { Name = "help", IsAvailable = true },
            new CommandMetadata { Name = "context", IsAvailable = true },
            new CommandMetadata { Name = "clear", IsAvailable = true }
        };

        // Act
        var result = _parser.GetSuggestions("/he", commands);

        // Assert
        result.Should().Contain("/help");
    }

    [Fact]
    public void FormatArguments_WithNoArguments_ReturnsEmpty()
    {
        // Act
        var result = _parser.FormatArguments(new List<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatArguments_WithSpacesInArg_QuotesArgument()
    {
        // Arrange
        var args = new List<string> { "hello world" };

        // Act
        var result = _parser.FormatArguments(args);

        // Assert
        result.Should().Be("\"hello world\"");
    }
}

/// <summary>
/// Unit tests for CommandRegistry
/// </summary>
public class CommandRegistryTests
{
    private readonly CommandRegistry _registry;
    private readonly TestLogger<CommandRegistry> _logger;

    public CommandRegistryTests()
    {
        _logger = new TestLogger<CommandRegistry>();
        _registry = new CommandRegistry(_logger);
    }

    [Fact]
    public void Register_AddsCommand_HandlerRetrievable()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test").Object;

        // Act
        _registry.Register(handler);

        // Assert
        _registry.GetHandler("test").Should().NotBeNull();
    }

    [Fact]
    public void Register_DuplicateCommand_ReplacesExisting()
    {
        // Arrange
        var handler1 = MockFactory.CreateMockCommandHandler("test").Object;
        var handler2 = MockFactory.CreateMockCommandHandler("test").Object;

        // Act
        _registry.Register(handler1);
        _registry.Register(handler2);

        // Assert
        _registry.CommandCount.Should().Be(1);
    }

    [Fact]
    public void Register_WithAliases_AliasesWork()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("help");
        handler.Setup(h => h.Metadata).Returns(new CommandMetadata
        {
            Name = "help",
            Aliases = new List<string> { "h", "?" }
        });

        // Act
        _registry.Register(handler.Object);

        // Assert
        _registry.GetHandler("h").Should().NotBeNull();
        _registry.GetHandler("?").Should().NotBeNull();
    }

    [Fact]
    public void Unregister_RemovesCommand()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test").Object;
        _registry.Register(handler);

        // Act
        var result = _registry.Unregister("test");

        // Assert
        result.Should().BeTrue();
        _registry.GetHandler("test").Should().BeNull();
    }

    [Fact]
    public void Unregister_UnknownCommand_ReturnsFalse()
    {
        // Act
        var result = _registry.Unregister("unknown");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCommand_ExistingCommand_ReturnsTrue()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test").Object;
        _registry.Register(handler);

        // Act & Assert
        _registry.HasCommand("test").Should().BeTrue();
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingCommands()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test");
        handler.Setup(h => h.Metadata).Returns(new CommandMetadata
        {
            Name = "test",
            Category = "Test"
        });
        _registry.Register(handler.Object);

        // Act
        var result = _registry.GetByCategory("Test");

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void GetCategories_ReturnsAllCategories()
    {
        // Arrange
        var handler1 = MockFactory.CreateMockCommandHandler("cmd1");
        handler1.Setup(h => h.Metadata).Returns(new CommandMetadata { Name = "cmd1", Category = "Cat1" });

        var handler2 = MockFactory.CreateMockCommandHandler("cmd2");
        handler2.Setup(h => h.Metadata).Returns(new CommandMetadata { Name = "cmd2", Category = "Cat2" });

        _registry.Register(handler1.Object);
        _registry.Register(handler2.Object);

        // Act
        var result = _registry.GetCategories();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Search_FindsMatchingCommands()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("help");
        handler.Setup(h => h.Metadata).Returns(new CommandMetadata
        {
            Name = "help",
            Description = "Displays help"
        });
        _registry.Register(handler.Object);

        // Act
        var result = _registry.Search("help");

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void GetHelpList_ReturnsFormattedHelp()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test");
        handler.Setup(h => h.Metadata).Returns(new CommandMetadata
        {
            Name = "test",
            Description = "Test command",
            Category = "Test"
        });
        _registry.Register(handler.Object);

        // Act
        var result = _registry.GetHelpList();

        // Assert
        result.Should().Contain("Available Commands");
        result.Should().Contain("Test");
    }

    [Fact]
    public void Clear_RemovesAllCommands()
    {
        // Arrange
        var handler = MockFactory.CreateMockCommandHandler("test").Object;
        _registry.Register(handler);

        // Act
        _registry.Clear();

        // Assert
        _registry.CommandCount.Should().Be(0);
    }
}
