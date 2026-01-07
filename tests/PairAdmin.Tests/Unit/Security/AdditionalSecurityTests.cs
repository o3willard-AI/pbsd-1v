using PairAdmin.Security;

namespace PairAdmin.Tests.Unit.Security;

/// <summary>
/// Unit tests for CommandValidationResult
/// </summary>
public class CommandValidationResultTests
{
    [Fact]
    public void CreateAllowed_ReturnsAllowedResult()
    {
        // Act
        var result = CommandValidationResult.Allowed();

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().BeNull();
        result.SuggestedCommand.Should().BeNull();
    }

    [Fact]
    public void CreateBlocked_ReturnsBlockedResult()
    {
        // Act
        var result = CommandValidationResult.Blocked("dangerous command", "/safe-alternative");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("dangerous command");
        result.SuggestedCommand.Should().Be("/safe-alternative");
    }

    [Fact]
    public void CreateBlockedWithoutSuggestion_Works()
    {
        // Act
        var result = CommandValidationResult.Blocked("dangerous command");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.SuggestedCommand.Should().BeNull();
    }
}

/// <summary>
/// Unit tests for DangerousCommandValidator
/// </summary>
public class DangerousCommandValidatorTests
{
    private readonly DangerousCommandValidator _validator;

    public DangerousCommandValidatorTests()
    {
        _validator = new DangerousCommandValidator();
    }

    [Fact]
    public void Validate_WithSafeCommand_ReturnsAllowed()
    {
        // Act
        var result = _validator.Validate("ls -la");

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithRmCommand_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate("rm -rf /tmp/*");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithSudoRmCommand_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate("sudo rm -rf /var/log");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithMkfsCommand_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate("mkfs.ext4 /dev/sda1");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithForkBomb_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate(":(){ :|:& };:");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithChmod777_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate("chmod 777 /etc/passwd");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithDDCommand_ReturnsBlocked()
    {
        // Act
        var result = _validator.Validate("dd if=/dev/zero of=/dev/sda");

        // Assert
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_CaseInsensitive_Works()
    {
        // Act
        var resultLower = _validator.Validate("rm -rf /tmp");
        var resultUpper = _validator.Validate("RM -RF /TMP");

        // Assert
        resultLower.IsAllowed.Should().BeFalse();
        resultUpper.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyCommand_ReturnsAllowed()
    {
        // Act
        var result = _validator.Validate("");

        // Assert
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void GetDangerousPatterns_ReturnsPatterns()
    {
        // Act
        var patterns = _validator.GetDangerousPatterns();

        // Assert
        patterns.Should().NotBeEmpty();
    }
}

/// <summary>
/// Unit tests for CommandValidators
/// </summary>
public class CommandValidatorsTests
{
    [Fact]
    public void CreateBasicValidator_ReturnsWorkingValidator()
    {
        // Act
        var validator = CommandValidators.CreateBasicValidator();

        // Assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void CreateStrictValidator_ReturnsWorkingValidator()
    {
        // Act
        var validator = CommandValidators.CreateStrictValidator();

        // Assert
        validator.Should().NotBeNull();
    }

    [Fact]
    public void CreatePermissiveValidator_ReturnsWorkingValidator()
    {
        // Act
        var validator = CommandValidators.CreatePermissiveValidator();

        // Assert
        validator.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for SecurityPolicy
/// </summary>
public class SecurityPolicyTests
{
    [Fact]
    public void DefaultPolicy_HasExpectedValues()
    {
        // Arrange & Act
        var policy = new SecurityPolicy();

        // Assert
        policy.FilterSensitiveData.Should().BeTrue();
        policy.BlockDangerousCommands.Should().BeTrue();
        policy.MaxCommandLength.Should().BeGreaterThan(0);
        policy.RequireConfirmationForDestructive.Should().BeTrue();
    }

    [Fact]
    public void CanModifyPolicy()
    {
        // Arrange
        var policy = new SecurityPolicy();

        // Act
        policy.FilterSensitiveData = false;
        policy.BlockDangerousCommands = false;

        // Assert
        policy.FilterSensitiveData.Should().BeFalse();
        policy.BlockDangerousCommands.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for AuditEntry
/// </summary>
public class AuditEntryTests
{
    [Fact]
    public void CanCreateAuditEntry()
    {
        // Arrange & Act
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventTypes.CommandExecuted,
            Command = "/help",
            UserId = "test-user",
            Details = new Dictionary<string, object> { { "key", "value" } }
        };

        // Assert
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entry.EventType.Should().Be(AuditEventTypes.CommandExecuted);
        entry.Command.Should().Be("/help");
        entry.UserId.Should().Be("test-user");
        entry.Details.Should().ContainKey("key");
    }

    [Fact]
    public void DefaultConstructor_SetsTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var entry = new AuditEntry();

        // Assert
        entry.Timestamp.Should().BeOnOrAfter(before);
    }
}

/// <summary>
/// Unit tests for AuditEventTypes
/// </summary>
public class AuditEventTypesTests
{
    [Fact]
    public void ContainsExpectedEvents()
    {
        // Assert
        AuditEventTypes.CommandExecuted.Should().NotBeNullOrEmpty();
        AuditEventTypes.CommandBlocked.Should().NotBeNullOrEmpty();
        AuditEventTypes.SensitiveDataFiltered.Should().NotBeNullOrEmpty();
        AuditEventTypes.SecurityViolation.Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// Unit tests for FilterPatterns
/// </summary>
public class FilterPatternsTests
{
    [Fact]
    public void RegexPattern_CanBeCreated()
    {
        // Arrange & Act
        var pattern = new RegexPattern(
            "Test Pattern",
            @"test\d+",
            preserveLength: 4);

        // Assert
        pattern.Name.Should().Be("Test Pattern");
        pattern.Pattern.Should().Be(@"test\d+");
        pattern.PreserveLength.Should().Be(4);
    }

    [Fact]
    public void KeywordPattern_CanBeCreated()
    {
        // Arrange & Act
        var pattern = new KeywordPattern(
            "Test Keyword",
            "secret",
            RedactionMethod.Mask);

        // Assert
        pattern.Name.Should().Be("Test Keyword");
        pattern.Keyword.Should().Be("secret");
        pattern.Method.Should().Be(RedactionMethod.Mask);
    }
}

/// <summary>
/// Unit tests for RedactionMethod
/// </summary>
public class RedactionMethodTests
{
    [Fact]
    public void ContainsExpectedMethods()
    {
        // Assert
        Enum.GetValues<RedactionMethod>().Should().Contain(RedactionMethod.Mask);
        Enum.GetValues<RedactionMethod>().Should().Contain(RedactionMethod.Remove);
        Enum.GetValues<RedactionMethod>().Should().Contain(RedactionMethod.Hash);
        Enum.GetValues<RedactionMethod>().Should().Contain(RedactionMethod.Placeholder);
    }
}
