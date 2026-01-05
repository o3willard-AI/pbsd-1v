using PairAdmin.Security;

namespace PairAdmin.Tests.Unit.Security;

/// <summary>
/// Unit tests for SensitiveDataFilter
/// </summary>
public class SensitiveDataFilterTests
{
    private readonly SensitiveDataFilter _filter;

    public SensitiveDataFilterTests()
    {
        _filter = FilterPatternFactory.CreateFilterWithDefaults();
    }

    [Fact]
    public void Filter_RedactsAwsKey_ReturnsMaskedValue()
    {
        // Arrange
        var input = "AWS_KEY=AKIAIOSFODNN7EXAMPLE";

        // Act
        var result = _filter.Filter(input);

        // Assert
        result.Should().Contain("****");
        result.Should().NotContain("AKIAIOSFODNN7");
    }

    [Fact]
    public void Filter_RedactsOpenAiKey_ReturnsMaskedValue()
    {
        // Arrange
        var input = "OPENAI_API_KEY=sk-abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRST";

        // Act
        var result = _filter.Filter(input);

        // Assert
        result.Should().Contain("****");
        result.Should().NotContain("sk-abcdefgh");
    }

    [Fact]
    public void Filter_RedactsEmail_ReturnsMaskedValue()
    {
        // Arrange
        var input = "Contact: test@example.com for support";

        // Act
        var result = _filter.Filter(input);

        // Assert
        result.Should().NotContain("test@example.com");
    }

    [Fact]
    public void Filter_NullInput_ReturnsNull()
    {
        // Act
        var result = _filter.Filter(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Filter_EmptyInput_ReturnsEmpty()
    {
        // Act
        var result = _filter.Filter("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Filter_NoSensitiveData_ReturnsOriginal()
    {
        // Arrange
        var input = "This is a normal message with no secrets";

        // Act
        var result = _filter.Filter(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Filter_MultipleSensitiveData_RedactsAll()
    {
        // Arrange
        var input = "Email: test@example.com, AWS: AKIA1234567890ABCDEF";

        // Act
        var result = _filter.Filter(input);

        // Assert
        result.Should().NotContain("test@example.com");
        result.Should().NotContain("AKIA1234567890ABCDEF");
    }

    [Fact]
    public void ContainsSensitiveData_WithSensitiveData_ReturnsTrue()
    {
        // Arrange
        var input = "API key: sk-abcdefghij";

        // Act
        var result = _filter.ContainsSensitiveData(input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsSensitiveData_WithoutSensitiveData_ReturnsFalse()
    {
        // Arrange
        var input = "This is a normal message";

        // Act
        var result = _filter.ContainsSensitiveData(input);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetStatistics_DetectsSensitiveData_ReturnsCount()
    {
        // Arrange
        var input = "Email: test@example.com";

        // Act
        var stats = _filter.GetStatistics(input);

        // Assert
        stats.HasSensitiveData.Should().BeTrue();
        stats.DetectionCount.Should().BeGreaterThan(0);
        stats.DetectedPatterns.Should().Contain("Email Address");
    }
}

/// <summary>
/// Unit tests for RegexPattern
/// </summary>
public class RegexPatternTests
{
    [Fact]
    public void Filter_WithValidPattern_ReturnsMaskedValue()
    {
        // Arrange
        var pattern = new RegexPattern(
            "Test",
            @"\d{3}",
            preserveLength: 0);
        var input = "Number: 12345";

        // Act
        var result = pattern.Filter(input, RedactionStrategy.Mask);

        // Assert
        result.Should().Be("Number: ***");
    }

    [Fact]
    public void Filter_WithPreserveLength_PreservesFirstCharacters()
    {
        // Arrange
        var pattern = new RegexPattern(
            "Test",
            @"\d{6}",
            preserveLength: 2);
        var input = "Code: 123456";

        // Act
        var result = pattern.Filter(input, RedactionStrategy.Mask);

        // Assert
        result.Should().Be("Code: 12****");
    }

    [Fact]
    public void Filter_WithRemoveStrategy_RemovesMatch()
    {
        // Arrange
        var pattern = new RegexPattern(
            "Test",
            @"secret",
            preserveLength: 0);
        var input = "The secret is out";

        // Act
        var result = pattern.Filter(input, RedactionStrategy.Remove);

        // Assert
        result.Should().Be("The  is out");
    }

    [Fact]
    public void Filter_WithPlaceholderStrategy_ReplacesWithPlaceholder()
    {
        // Arrange
        var pattern = new RegexPattern(
            "Test",
            @"password123",
            preserveLength: 0,
            placeholder: "[HIDDEN]");
        var input = "Password: password123";

        // Act
        var result = pattern.Filter(input, RedactionStrategy.Placeholder);

        // Assert
        result.Should().Be("Password: [HIDDEN]");
    }

    [Fact]
    public void ContainsSensitiveData_WithMatch_ReturnsTrue()
    {
        // Arrange
        var pattern = new RegexPattern("Test", @"\d+", preserveLength: 0);

        // Act & Assert
        pattern.ContainsSensitiveData("123").Should().BeTrue();
        pattern.ContainsSensitiveData("abc").Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_False_IgnoresPattern()
    {
        // Arrange
        var pattern = new RegexPattern("Test", @"\d+", preserveLength: 0)
        {
            IsEnabled = false
        };
        var input = "123";

        // Act
        var result = pattern.Filter(input, RedactionStrategy.Mask);

        // Assert
        result.Should().Be("123");
    }
}

/// <summary>
/// Unit tests for CommandValidationService
/// </summary>
public class CommandValidationServiceTests
{
    private readonly CommandValidationService _service;
    private readonly SecurityPolicy _policy;

    public CommandValidationServiceTests()
    {
        _policy = SecurityPolicy.CreateDefault();
        _service = new CommandValidationService(_policy);
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = "/help";
        var context = new TestContext().ToCommandContext();

        // Act
        var result = _service.Validate(command, context);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be(CommandValidationStatus.Allowed);
    }

    [Fact]
    public void Validate_EmptyCommand_ReturnsForbidden()
    {
        // Arrange
        var command = "";
        var context = new TestContext().ToCommandContext();

        // Act
        var result = _service.Validate(command, context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(CommandValidationStatus.Forbidden);
    }

    [Fact]
    public void Validate_BlacklistedCommand_ReturnsForbidden()
    {
        // Arrange
        _policy.BlacklistedCommands.Add("rm");
        var command = "/rm -rf /";
        var context = new TestContext().ToCommandContext();

        // Act
        var result = _service.Validate(command, context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be(CommandValidationStatus.Forbidden);
    }

    [Fact]
    public void Validate_DisabledMode_ReturnsSuccess()
    {
        // Arrange
        _policy.Mode = ValidationMode.Disabled;
        var command = "/rm -rf /";
        var context = new TestContext().ToCommandContext();

        // Act
        var result = _service.Validate(command, context);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RequiresElevatedPrivilege_WithElevatedCommand_ReturnsTrue()
    {
        // Arrange
        _policy.CommandPrivilegeRequirements["mode"] = PrivilegeLevel.Elevated;

        // Act & Assert
        _service.RequiresElevatedPrivilege("/mode").Should().BeTrue();
        _service.RequiresElevatedPrivilege("/help").Should().BeFalse();
    }

    [Fact]
    public void IsBlacklisted_WithBlacklistedCommand_ReturnsTrue()
    {
        // Arrange
        _policy.BlacklistedCommands.Add("dangerous");

        // Act & Assert
        _service.IsBlacklisted("/dangerous").Should().BeTrue();
        _service.IsBlacklisted("/help").Should().BeFalse();
    }

    [Fact]
    public void GetPolicy_ReturnsConfiguredPolicy()
    {
        // Act
        var policy = _service.GetPolicy();

        // Assert
        policy.Should().NotBeNull();
        policy.Mode.Should().Be(ValidationMode.Blacklist);
    }
}
