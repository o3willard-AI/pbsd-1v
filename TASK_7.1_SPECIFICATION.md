# Task 7.1 Specification: Sensitive Data Filtering

## Task: Implement Sensitive Data Filter Service

**Phase:** Phase 7: Security  
**Status:** In Progress  
**Date:** January 4, 2026  
**Prerequisites:** Task 6.4 (/filter command) complete

---

## Description

Implement a comprehensive sensitive data filtering service that automatically detects and redacts sensitive information from terminal output before it's sent to the LLM or displayed in chat. This prevents accidental leakage of credentials, API keys, passwords, and other secrets.

---

## Deliverables

### 1. SensitiveDataFilter.cs
Core filtering service:
- Pattern detection (regex and text)
- Multiple redaction strategies
- Filter chain support
- Performance optimization

### 2. DefaultPatterns.cs
Built-in sensitive data patterns:
- API keys (AWS, OpenAI, GitHub, etc.)
- Passwords and secrets
- Private keys
- Credit card numbers
- Email addresses
- Custom patterns

### 3. IFilterChain.cs
Filter chain management:
- Ordered filter execution
- Conditional filtering
- Error handling

---

## Requirements

### Functional Requirements

#### Pattern Detection
| Requirement | Description |
|-------------|-------------|
| API Key Detection | Detect AWS keys, OpenAI keys, GitHub tokens |
| Password Detection | Detect password assignments |
| Private Key Detection | Detect RSA/SSH private keys |
| Credit Card Detection | Detect credit card numbers |
| Email Detection | Detect email addresses |
| Custom Patterns | User-defined filter patterns |

#### Redaction Strategies
| Strategy | Description |
|----------|-------------|
| Mask | Replace with asterisks (e.g., `****`) |
| Remove | Remove the matched text entirely |
| Hash | Replace with hash value |
| Placeholder | Replace with custom text (e.g., `[REDACTED]`) |

#### Performance Requirements
| Requirement | Description |
|-------------|-------------|
| Fast Detection | Sub-millisecond for typical text |
| Streaming Support | Handle large terminal outputs |
| Memory Efficient | No memory leaks for long sessions |

### Non-Functional Requirements

1. **Accuracy**
   - Minimize false positives
   - No false negatives for known patterns

2. **Performance**
   - < 1ms for typical 10KB context
   - Streaming support for large outputs

3. **Extensibility**
   - Easy to add new patterns
   - Customizable redaction strategies

4. **Security**
   - No logging of sensitive data
   - Secure memory handling

---

## Implementation

### SensitiveDataFilter Class

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Service for detecting and redacting sensitive data
/// </summary>
public class SensitiveDataFilter
{
    private readonly List<IFilterPattern> _patterns;
    private readonly RedactionStrategy _defaultStrategy;

    public SensitiveDataFilter(RedactionStrategy strategy = RedactionStrategy.Mask)
    {
        _patterns = new List<IFilterPattern>();
        _defaultStrategy = strategy;
    }

    /// <summary>
    /// Filters sensitive data from text
    /// </summary>
    public string Filter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        foreach (var pattern in _patterns)
        {
            text = pattern.Filter(text, _defaultStrategy);
        }

        return text;
    }

    /// <summary>
    /// Filters sensitive data from a list of messages
    /// </summary>
    public IEnumerable<ChatMessage> FilterMessages(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            yield return new ChatMessage
            {
                Role = message.Role,
                Content = Filter(message.Content),
                Timestamp = message.Timestamp
            };
        }
    }

    /// <summary>
    /// Adds a custom pattern
    /// </summary>
    public void AddPattern(IFilterPattern pattern)
    {
        _patterns.Add(pattern);
    }
}
```

### IFilterPattern Interface

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Interface for filter patterns
/// </summary>
public interface IFilterPattern
{
    string Name { get; }
    string Description { get; }
    bool IsEnabled { get; set; }

    string Filter(string text, RedactionStrategy strategy);
    bool ContainsSensitiveData(string text);
}
```

### RedactionStrategy Enum

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Strategy for redacting sensitive data
/// </summary>
public enum RedactionStrategy
{
    Mask,          // **** or ****abcd
    Remove,        // (empty string)
    Hash,          // #HashValue
    Placeholder    // [REDACTED]
}
```

### RegexPattern Class

```csharp
namespace PairAdmin.Security;

/// <summary>
/// Regex-based filter pattern
/// </summary>
public class RegexPattern : IFilterPattern
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    private readonly Regex _regex;
    private readonly int _preserveLength;

    public RegexPattern(string name, string pattern, int preserveLength = 4)
    {
        Name = name;
        Description = $"Regex pattern for {name}";
        _regex = new Regex(pattern, RegexOptions.Compiled);
        _preserveLength = preserveLength;
    }

    public string Filter(string text, RedactionStrategy strategy)
    {
        return _regex.Replace(text, match => GetReplacement(match, strategy));
    }

    private string GetReplacement(Match match, RedactionStrategy strategy)
    {
        return strategy switch
        {
            RedactionStrategy.Mask => MaskMatch(match),
            RedactionStrategy.Remove => string.Empty,
            RedactionStrategy.Hash => HashValue(match.Value),
            RedactionStrategy.Placeholder => "[REDACTED]",
            _ => match.Value
        };
    }
}
```

---

## Default Patterns

### API Keys and Tokens

```csharp
public static class DefaultPatterns
{
    public static List<IFilterPattern> GetDefaultPatterns()
    {
        return new List<IFilterPattern>
        {
            // AWS Access Key ID
            new RegexPattern(
                "AWS Access Key",
                @"AKIA[0-9A-Z]{16}",
                preserveLength: 4),

            // AWS Secret Key
            new RegexPattern(
                "AWS Secret Key",
                @"[0-9a-zA-Z/+]{40}"),

            // OpenAI API Key
            new RegexPattern(
                "OpenAI API Key",
                @"sk-[a-zA-Z0-9]{48}",
                preserveLength: 7),

            // GitHub Personal Access Token
            new RegexPattern(
                "GitHub Token",
                @"ghp_[a-zA-Z0-9]{36}",
                preserveLength: 4),

            // Generic API Key
            new RegexPattern(
                "API Key",
                @"(?i)(api_key|apikey|api-key)[\s=:]+[^\s]+",
                preserveLength: 0),

            // Password assignment
            new RegexPattern(
                "Password",
                @"(?i)(password|passwd|pwd)[\s=:]+[^\s]+",
                preserveLength: 0),
        };
    }
}
```

---

## Integration Points

### With ContextWindowManager (Task 4.1)
```csharp
// Before adding context to request
var filteredContext = sensitiveDataFilter.Filter(rawContext);
contextManager.SetContext(filteredContext);
```

### With FilterCommandHandler (Task 6.4)
```csharp
// Extend existing filter handler
var filterHandler = new FilterCommandHandler();
filterHandler.AddPatterns(DefaultPatterns.GetDefaultPatterns());
```

### With ChatPane (Task 3.1)
```csharp
// Filter messages before display
var filteredMessages = sensitiveDataFilter.FilterMessages(messages);
chatPane.UpdateMessages(filteredMessages);
```

---

## Performance Considerations

### Optimization Strategies
1. **Compiled Regex** - Use RegexOptions.Compiled
2. **Lazy Evaluation** - Only filter when needed
3. **Streaming** - Support large inputs
4. **Caching** - Cache compiled patterns

### Benchmarks
| Input Size | Expected Time |
|------------|---------------|
| 1 KB | < 1 ms |
| 10 KB | < 5 ms |
| 100 KB | < 50 ms |
| 1 MB | < 500 ms |

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Invalid regex pattern | Log warning, skip pattern |
| Timeout | Abort filtering, log error |
| Memory pressure | Skip large inputs |
| Pattern conflict | Use first match |

---

## Testing

### Unit Tests

```csharp
[Fact]
public void Filter_RedactsAWSKey()
{
    // Arrange
    var filter = new SensitiveDataFilter();
    filter.AddPattern(new RegexPattern("AWS", @"AKIA[0-9A-Z]{16}"));
    var input = "AWS_KEY=AKIAIOSFODNN7EXAMPLE";

    // Act
    var result = filter.Filter(input);

    // Assert
    Assert.Contains("****", result);
    Assert.DoesNotContain("AKIAIOSFODNN7", result);
}

[Fact]
public void Filter_PreservesNonSensitive()
{
    // Arrange
    var filter = new SensitiveDataFilter();
    filter.AddPattern(DefaultPatterns.GetDefaultPatterns());
    var input = "Hello, this is a normal message.";

    // Act
    var result = filter.Filter(input);

    // Assert
    Assert.Equal(input, result);
}

[Fact]
public void Filter_MultiplePatterns_RedactsAll()
{
    // Arrange
    var filter = new SensitiveDataFilter();
    filter.AddPattern(new RegexPattern("Email", @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"));
    filter.AddPattern(new RegexPattern("Phone", @"\d{3}-\d{3}-\d{4}"));
    var input = "Contact: test@email.com, Phone: 555-123-4567";

    // Act
    var result = filter.Filter(input);

    // Assert
    Assert.DoesNotContain("test@email.com", result);
    Assert.DoesNotContain("555-123-4567", result);
}
```

---

## Acceptance Criteria

- [ ] Detects AWS access keys
- [ ] Detects OpenAI API keys
- [ ] Detects GitHub tokens
- [ ] Detects password assignments
- [ ] Supports custom patterns
- [ ] Supports multiple redaction strategies
- [ ] Filters in < 1ms for 10KB text
- [ ] No sensitive data in logs
- [ ] Integrates with ContextManager
- [ ] Integrates with FilterCommandHandler

---

## Files Created

```
src/Security/
├── SensitiveDataFilter.cs      # Main filter service
├── IFilterPattern.cs           # Pattern interface
├── RegexPattern.cs             # Regex implementation
├── DefaultPatterns.cs          # Built-in patterns
└── RedactionStrategy.cs        # Strategy enum
```

---

## Estimated Complexity

| File | Complexity | Lines |
|------|------------|-------|
| SensitiveDataFilter.cs | Medium | ~150 |
| IFilterPattern.cs | Low | ~50 |
| RegexPattern.cs | Medium | ~100 |
| DefaultPatterns.cs | Low | ~100 |
| RedactionStrategy.cs | Low | ~20 |

**Total Estimated:** ~420 lines of C#

---

## Next Steps

After Task 7.1 is complete:
1. Task 7.2: Command Validation
2. Task 7.3: Audit Logging
3. Phase 7 Complete Summary

---

## Notes

- Use RegexOptions.Compiled for performance
- Add XML documentation for all public types
- Consider adding performance benchmarks
- Ensure no sensitive data in logs
- Support both sync and async filtering
