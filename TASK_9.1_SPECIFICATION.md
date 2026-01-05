# Task 9.1 Specification: Test Infrastructure

## Task: Implement Test Infrastructure

**Phase:** Phase 9: Testing & QA  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** None

---

## Description

Set up comprehensive test infrastructure including test framework, utilities, mock services, and test data factories to enable unit and integration testing for all PairAdmin components.

---

## Deliverables

### 1. Test Project Setup
- xUnit test framework
- Test project configuration
- Code coverage setup (Coverlet)

### 2. Test Utilities
- TestLogger<T>
- TestHttpClientFactory
- Assertion helpers
- Async test helpers

### 3. Mock Services
- Mock<ILLMProvider>
- Mock<IContextProvider>
- Mock<ICommandHandler>
- Mock<ILogger>

### 4. Test Data Factories
- ChatMessageFactory
- CommandContextFactory
- UserSettingsFactory
- ProviderConfigurationFactory

---

## Requirements

### Test Framework Setup

```xml
<!-- tests/PairAdmin.Tests/PairAdmin.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\PairAdmin\PairAdmin.csproj" />
  </ItemGroup>
</Project>
```

### Test Utilities

```csharp
namespace PairAdmin.Tests.Utilities;

/// <summary>
/// Test logger that captures log entries
/// </summary>
public class TestLogger<T> : ILogger<T>, IDisposable
{
    private readonly List<LogEntry> _entries = new();
    private readonly ILogger<T>? _innerLogger;

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger?.BeginScope(state) ?? NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger?.IsEnabled(logLevel) ?? true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _entries.Add(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel,
            Message = message,
            Exception = exception,
            Category = typeof(T).Name
        });

        _innerLogger?.Log(logLevel, eventId, state, exception, formatter);
    }

    public void Dispose()
    {
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? EventId { get; set; }
}

/// <summary>
/// Helpers for async testing
/// </summary>
public static class AsyncTestHelpers
{
    /// <summary>
    /// Wait for a condition with timeout
    /// </summary>
    public static async Task<bool> WaitForAsync(
        Func<bool> condition,
        int timeoutMs = 5000,
        int pollIntervalMs = 100)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(timeoutMs))
        {
            if (condition())
                return true;

            await Task.Delay(pollIntervalMs);
        }

        return false;
    }

    /// <summary>
    /// Execute with retry on failure
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        int delayMs = 100)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs * attempt);
                }
            }
        }

        throw lastException!;
    }

    /// <summary>
    /// Create a cancelled cancellation token
    /// </summary>
    public static CancellationToken CreateCancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }
}
```

### Mock Services

```csharp
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
                Description = "Test provider",
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

        mock.Setup(p => p.EstimateTokenCount(It.IsAny<string>()))
            .Returns((string text) => text.Length / 4);

        return mock;
    }

    /// <summary>
    /// Creates a mock command handler
    /// </summary>
    public static Mock<ICommandHandler> CreateMockCommandHandler(
        string commandName = "test",
        CommandResult? result = null)
    {
        var mock = new Mock<ICommandHandler>();

        mock.Setup(h => h.Metadata)
            .Returns(new CommandMetadata
            {
                Name = commandName,
                Description = "Test command",
                Category = "Test",
                IsAvailable = true
            });

        mock.Setup(h => h.Execute(It.IsAny<CommandContext>(), It.IsAny<ParsedCommand>()))
            .Returns(result ?? CommandResult.Success("Test result"));

        mock.Setup(h => h.GetHelpText())
            .Returns($"## /{commandName}\n\nTest command help");

        mock.Setup(h => h.CanExecute(It.IsAny<CommandContext>()))
            .Returns(true);

        return mock;
    }

    /// <summary>
    /// Creates a mock logger
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}

/// <summary>
/// In-memory test context
/// </summary>
public class TestContext
{
    public List<ChatMessage> Messages { get; } = new();
    public UserSettings Settings { get; } = new();
    public Mock<ILLMProvider> LLMProvider { get; } = MockFactory.CreateMockLLMProvider();
    public ContextWindowManager? ContextManager { get; set; }
    public ChatPane? ChatPane { get; set; }
    public CancellationToken CancellationToken { get; } = CancellationToken.None;

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

    private LLMGateway.LLMGateway CreateTestGateway()
    {
        var gateway = new LLMGateway.LLMGateway(
            new Mock<ILogger<LLMGateway.LLMGateway>>().Object);

        gateway.RegisterProvider(LLMProvider.Object);

        return gateway;
    }
}
```

### Test Data Factories

```csharp
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
}
```

---

## Test Categories

### Unit Tests
- Command handlers
- Services (without dependencies)
- Data models
- Utilities

### Integration Tests
- Service combinations
- Handler + Dispatcher
- Filter + Context
- Help services

### Performance Tests
- Large message filtering
- High-frequency commands
- Memory usage

---

## Files Created

```
tests/PairAdmin.Tests/
├── PairAdmin.Tests.csproj              # Test project
├── Utilities/
│   ├── TestLogger.cs                   # Test logger
│   ├── AsyncTestHelpers.cs             # Async helpers
│   └── AssertionExtensions.cs          # Custom assertions
├── Mocks/
│   ├── MockFactory.cs                  # Mock factory
│   └── TestContext.cs                  # Test context
├── Factories/
│   ├── ChatMessageFactory.cs           # Message factory
│   ├── UserSettingsFactory.cs          # Settings factory
│   └── ParsedCommandFactory.cs         # Command factory
└── GlobalUsings.cs                     # Using directives
```

---

## Test Configuration

### coverlet.json
```json
{
  "mergeWith": "",
  "exclude": [
    "[*]*.Tests.*",
    "[*]*.Tests",
    "[*]Tests.*",
    "[xunit.*]",
    "[Microsoft.*]",
    "[System.*]"
  ],
  "include": [
    "[PairAdmin]*"
  ],
  "source": [
    "../../src/PairAdmin"
  ]
}
```

### .editorconfig test rules
```
# Disable CA2007 for test projects
dotnet_code_quality.CA2007.skip_for_symbols = true

# Allow test method names
dotnet_naming_rule.test_method_should_bePascalCase.severity = none
```

---

## Next Steps

After Task 9.1 is complete:
1. Task 9.2: Unit Tests
2. Task 9.3: Integration Tests
3. Phase 9 Complete Summary

---

## Notes

- Use FluentAssertions for readable assertions
- Mock external dependencies with Moq
- Keep tests fast (< 100ms each)
- Follow Arrange-Act-Assert pattern
- Use descriptive test names
- Group related tests with test classes
