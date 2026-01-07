using PairAdmin.Commands;
using PairAdmin.Configuration;

namespace PairAdmin.Tests.Unit.Commands;

/// <summary>
/// Unit tests for ThemeCommandHandler
/// </summary>
public class ThemeCommandHandlerTests
{
    private readonly ThemeCommandHandler _handler;
    private readonly Mock<SettingsProvider> _settingsProvider;
    private readonly TestContext _context;

    public ThemeCommandHandlerTests()
    {
        var logger = new TestLogger<ThemeCommandHandler>();
        _settingsProvider = new Mock<SettingsProvider>(logger.Object);
        _handler = new ThemeCommandHandler(_settingsProvider.Object, logger);
        _context = new TestContext();
    }

    [Fact]
    public void Execute_WithNoArguments_ShowsCurrentTheme()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme");

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Current Theme Settings");
        result.Response.Should().Contain("Mode");
        result.Response.Should().Contain("Accent Color");
    }

    [Fact]
    public void Execute_WithLightArgument_SetsLightTheme()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string> { "light" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Theme Updated");
        result.Response.Should().Contain("light");
    }

    [Fact]
    public void Execute_WithDarkArgument_SetsDarkTheme()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string> { "dark" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("dark");
    }

    [Fact]
    public void Execute_WithSystemArgument_SetsSystemTheme()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string> { "system" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("system");
        result.Response.Should().Contain("match your system theme");
    }

    [Fact]
    public void Execute_WithValidColor_SetsAccentColor()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string> { "#3498DB" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("#3498DB");
    }

    [Fact]
    public void Execute_WithInvalidColor_ReturnsError()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string> { "#XYZ123" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Response.Should().Contain("Invalid color format");
    }

    [Fact]
    public void Execute_WithInvalidHexColor_ReturnsError()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string> { "#GGGGGG" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Execute_WithFontSizeFlag_SetsFontSize()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string>(), new Dictionary<string, string> { ["font-size"] = "16" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("16px");
    }

    [Fact]
    public void Execute_WithInvalidFontSize_ReturnsError()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string>(), new Dictionary<string, string> { ["font-size"] = "100" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Response.Should().Contain("between 8 and 72");
    }

    [Fact]
    public void Execute_WithTooSmallFontSize_ReturnsError()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string>(), new Dictionary<string, string> { ["font-size"] = "5" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Execute_WithSaveError_ReturnsError()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));
        var command = ParsedCommandFactory.Create("theme", new List<string> { "light" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Response.Should().Contain("Save failed");
    }

    [Fact]
    public void Metadata_HasCorrectValues()
    {
        // Assert
        _handler.Metadata.Name.Should().Be("theme");
        _handler.Metadata.Category.Should().Be("Configuration");
        _handler.Metadata.Aliases.Should().Contain("t");
        _handler.Metadata.Aliases.Should().Contain("color");
        _handler.Metadata.MinArguments.Should().Be(0);
        _handler.Metadata.MaxArguments.Should().Be(1);
    }

    [Fact]
    public void Execute_WithShowArgument_ShowsCurrentTheme()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string> { "show" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Response.Should().Contain("Current Theme Settings");
    }

    [Fact]
    public void Execute_WithUnknownArgument_ReturnsError()
    {
        // Arrange
        var command = ParsedCommandFactory.Create("theme", new List<string> { "unknown" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(CommandStatus.InvalidArguments);
    }

    [Fact]
    public void Execute_With9DigitHexColor_SetsAccentColor()
    {
        // Arrange
        _settingsProvider.Setup(sp => sp.SaveSettingsAsync(It.IsAny<UserSettings>()))
            .Returns(Task.CompletedTask);
        var command = ParsedCommandFactory.Create("theme", new List<string> { "#FF5733FF" });

        // Act
        var result = _handler.Execute(_context.ToCommandContext(), command);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for SettingsProvider
/// </summary>
public class SettingsProviderTests
{
    private readonly TestLogger<SettingsProvider> _logger;
    private readonly string _testSettingsPath;
    private SettingsProvider _settingsProvider;

    public SettingsProviderTests()
    {
        _logger = new TestLogger<SettingsProvider>();
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
        _settingsProvider = new SettingsProvider(_logger.Object, _testSettingsPath);
    }

    [Fact]
    public async Task LoadSettingsAsync_WithNoFile_ReturnsDefaults()
    {
        // Arrange
        if (File.Exists(_testSettingsPath))
            File.Delete(_testSettingsPath);

        // Act
        var settings = await _settingsProvider.LoadSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.Version.Should().Be(1);
        settings.Theme.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesFile()
    {
        // Arrange
        var settings = UserSettingsFactory.Create();

        // Act
        await _settingsProvider.SaveSettingsAsync(settings);

        // Assert
        File.Exists(_testSettingsPath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadSettingsAsync_WithFile_ReturnsSavedSettings()
    {
        // Arrange
        var settings = UserSettingsFactory.Create();
        settings.Theme!.Mode = ThemeMode.Dark;
        settings.Theme.AccentColor = "#FF0000";
        await _settingsProvider.SaveSettingsAsync(settings);

        var newProvider = new SettingsProvider(_logger.Object, _testSettingsPath);

        // Act
        var loadedSettings = await newProvider.LoadSettingsAsync();

        // Assert
        loadedSettings.Theme!.Mode.Should().Be(ThemeMode.Dark);
        loadedSettings.Theme.AccentColor.Should().Be("#FF0000");
    }

    [Fact]
    public void GetCachedSettings_ReturnsCachedSettings()
    {
        // Arrange
        var settings = UserSettingsFactory.Create();
        _settingsProvider.SaveSettingsAsync(settings).Wait();

        // Act
        var cached = _settingsProvider.GetCachedSettings();

        // Assert
        cached.Should().NotBeNull();
        cached!.Theme!.Mode.Should().Be(settings.Theme!.Mode);
    }

    [Fact]
    public void ResetToDefaults_ReturnsNewSettings()
    {
        // Act
        var defaults = _settingsProvider.ResetToDefaults();

        // Assert
        defaults.Should().NotBeNull();
        defaults.Theme!.Mode.Should().Be(ThemeMode.System);
    }

    [Fact]
    public void GetSettingsPath_ReturnsConfiguredPath()
    {
        // Act
        var path = _settingsProvider.GetSettingsPath();

        // Assert
        path.Should().Be(_testSettingsPath);
    }

    [Fact]
    public void SettingsFileExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var settings = UserSettingsFactory.Create();
        _settingsProvider.SaveSettingsAsync(settings).Wait();

        // Act
        var exists = _settingsProvider.SettingsFileExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void SettingsFileExists_WithNoFile_ReturnsFalse()
    {
        // Arrange
        var noFileProvider = new SettingsProvider(_logger.Object, "/nonexistent/path/settings.json");

        // Act
        var exists = noFileProvider.SettingsFileExists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void DeleteSettings_RemovesFile()
    {
        // Arrange
        var settings = UserSettingsFactory.Create();
        _settingsProvider.SaveSettingsAsync(settings).Wait();

        // Act
        _settingsProvider.DeleteSettings();

        // Assert
        File.Exists(_testSettingsPath).Should().BeFalse();
    }

    [Fact]
    public async Task LoadSettingsAsync_WithInvalidJson_ReturnsDefaults()
    {
        // Arrange
        await File.WriteAllTextAsync(_testSettingsPath, "{ invalid json }");

        // Act
        var settings = await _settingsProvider.LoadSettingsAsync();

        // Assert
        settings.Should().NotBeNull();
        settings.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithException_Throws()
    {
        // Arrange
        var invalidPathProvider = new SettingsProvider(_logger.Object, "/invalid/directory/settings.json");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invalidPathProvider.SaveSettingsAsync(UserSettingsFactory.Create()));
    }

    public void Dispose()
    {
        if (File.Exists(_testSettingsPath))
        {
            try { File.Delete(_testSettingsPath); } catch { }
        }
    }
}

/// <summary>
/// Unit tests for ThemeMode enum
/// </summary>
public class ThemeModeTests
{
    [Fact]
    public void ThemeMode_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ThemeMode>().Should().Contain(ThemeMode.Light);
        Enum.GetValues<ThemeMode>().Should().Contain(ThemeMode.Dark);
        Enum.GetValues<ThemeMode>().Should().Contain(ThemeMode.System);
    }
}

/// <summary>
/// Unit tests for ThemeSettings
/// </summary>
public class ThemeSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new ThemeSettings();

        // Assert
        settings.Mode.Should().Be(ThemeMode.System);
        settings.AccentColor.Should().Be("#3498DB");
        settings.FontSize.Should().Be(14);
        settings.FontFamily.Should().Be("Segoe UI");
    }

    [Fact]
    public void CanSetProperties()
    {
        // Arrange
        var settings = new ThemeSettings();

        // Act
        settings.Mode = ThemeMode.Dark;
        settings.AccentColor = "#FF0000";
        settings.FontSize = 18;
        settings.FontFamily = "Consolas";

        // Assert
        settings.Mode.Should().Be(ThemeMode.Dark);
        settings.AccentColor.Should().Be("#FF0000");
        settings.FontSize.Should().Be(18);
        settings.FontFamily.Should().Be("Consolas");
    }
}
