# Task 4.1 Specification: Context Window Management

## Task: Implement Context Window Management

**Phase:** Phase 4: Context Awareness
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Implement flexible context window management that allows users to configure how much terminal context is sent to the AI. This includes dynamic context truncation, limit enforcement, and configuration persistence.

---

## Deliverables

### 1. ContextSizePolicy.cs
Policy for managing context window size:
- Size modes (auto, fixed, percentage)
- Min/max constraints
- Model-specific limits

### 2. ContextWindowManager.cs
Main manager for context window:
- Applies size policy to context
- Handles context truncation
- Enforces token limits
- Integrates with IContextProvider

### 3. UserSettings.cs
User configuration management:
- Context window settings
- Application settings
- Persistence (save/load)
- Default values

---

## Requirements

### Functional Requirements

1. **Context Size Configuration**
   - User can set max context size
   - Multiple size modes (auto, fixed, percentage)
   - Model-specific default sizes
   - Min/max constraints

2. **Context Truncation**
   - Truncate context when exceeding limits
   - Smart truncation (preserve recent messages)
   - Preserve important information
   - Handle edge cases

3. **Limit Enforcement**
   - Respect model context limits
   - Prevent requests exceeding limits
   - Provide feedback on truncation
   - Token counting accuracy

4. **Configuration Persistence**
   - Save settings across sessions
   - Load settings on startup
   - Handle missing/invalid settings
   - Provide reset to defaults

### Non-Functional Requirements

1. **Performance**
   - Efficient truncation algorithms
   - Minimal overhead
   - Fast configuration updates

2. **Usability**
   - Clear feedback on context size
   - Reasonable defaults
   - Easy to configure

3. **Reliability**
   - Consistent behavior
   - Error handling
   - Graceful degradation

---

## Data Models

### ContextSizeMode
```csharp
public enum ContextSizeMode
{
    Auto,       // Automatically determine based on model
    Fixed,      // Fixed number of tokens/lines
    Percentage   // Percentage of model's max context
}
```

### ContextSizePolicy
```csharp
public class ContextSizePolicy
{
    public ContextSizeMode Mode { get; set; }
    public int FixedSize { get; set; }
    public double Percentage { get; set; }
    public int MinLines { get; set; }
    public int MaxLines { get; set; }
    public int MinTokens { get; set; }
    public int MaxTokens { get; set; }

    int GetTargetSize(int modelMaxContext);
    bool Validate(out string? errorMessage);
}
```

### ContextWindowSettings
```csharp
public class ContextWindowSettings
{
    public string ProviderId { get; set; }
    public string Model { get; set; }
    public ContextSizeMode SizeMode { get; set; }
    public int FixedSize { get; set; }
    public double Percentage { get; set; }
    public bool IncludeSystemPrompt { get; set; }
    public bool IncludeWorkingDirectory { get; set; }
    public bool IncludePrivilegeLevel { get; set; }
}
```

### TruncationResult
```csharp
public class TruncationResult
{
    public string TruncatedContext { get; set; }
    public int OriginalTokens { get; set; }
    public int TruncatedTokens { get; set; }
    public int RemovedTokens { get; set; }
    public bool WasTruncated { get; set; }
    public TruncationReason Reason { get; set; }
}

public enum TruncationReason
{
    None,
    TokenLimit,
    LineLimit,
    ModelLimit,
    UserLimit
}
```

---

## Architecture

```
ContextWindowManager
├── ContextSizePolicy (Policy)
│   ├── Size Mode (Auto/Fixed/Percentage)
│   ├── Constraints (Min/Max)
│   └── Model-specific Limits
├── IContextProvider (Source)
└── Truncation Logic
    ├── Token Counting
    ├── Smart Truncation
    └── Feedback Generation
```

### Truncation Strategy

1. **Auto Mode:**
   - Use model's default context window
   - Adjust based on model type
   - Respect provider limits

2. **Fixed Mode:**
   - Use exact number of tokens/lines
   - Enforce min/max constraints
   - Provide clear feedback

3. **Percentage Mode:**
   - Use percentage of model's max context
   - Calculate: `modelMax * percentage`
   - Round to nearest reasonable value

### Smart Truncation

```
Original Context:
  [System Prompt]
  [Message 1]
  [Message 2]
  [Message 3]
  [Message 4]

Truncated Context (if needed):
  [System Prompt]
  [Message 3]
  [Message 4]

Strategy: Keep most recent messages while preserving system prompt
```

---

## Integration Points

### Dependencies
- Task 2.4: IContextProvider ✅
- Task 3.2: LLMGateway (model info)
- Task 4.2: Token counter (to be implemented)

### Integration with IContextProvider
```csharp
var contextManager = new ContextWindowManager(contextProvider, settings);
var truncatedContext = contextManager.GetTruncatedContext(request.MaxTokens);
request.AddContext(truncatedContext);
```

### Integration with LLMGateway
```csharp
var modelInfo = gateway.GetProviderInfo(providerId);
var settings = userSettings.GetContextSettings(providerId, modelInfo.Model);
var policy = new ContextSizePolicy(settings, modelInfo);
var targetSize = policy.GetTargetSize(modelInfo.MaxContextTokens);
```

---

## Technical Considerations

### Token Counting
- Use IContextProvider's token count
- Fallback to estimation if not available
- Model-specific adjustments when known

### Truncation Algorithm
1. Count tokens in full context
2. If under limit, return full context
3. If over limit:
   a. Keep system prompt (if any)
   b. Keep most recent messages
   c. Stop at target size
   d. Mark as truncated

### Feedback
- Log truncation events
- Show warning in UI
- Track truncation statistics
- Provide option to disable truncation

### Model-Specific Defaults
- GPT-3.5: 4096 tokens
- GPT-4: 8192 tokens
- Claude: 100000 tokens
- Local models: vary

---

## Acceptance Criteria

- [ ] Context size can be changed via configuration
- [ ] Context is properly truncated when needed
- [ ] Settings persist across sessions
- [ ] Auto mode selects appropriate size
- [ ] Fixed mode uses exact size
- [ ] Percentage mode calculates correctly
- [ ] Min/max constraints are enforced
- [ ] System prompt is preserved during truncation
- [ ] Most recent messages are preserved
- [ ] Truncation feedback is provided
- [ ] Model-specific limits are respected
- [ ] Integration with IContextProvider works

---

## Next Steps

After Task 4.1 is complete:
1. Task 4.2: Add Token Counting and Context Meter UI Component
2. Task 4.3: Parse and Track Current Working Directory
3. Task 4.4: Detect and Track User Privilege Levels

---

## Files to Create

```
/src/Context/ContextSizePolicy.cs
/src/Context/ContextWindowManager.cs
/src/Configuration/UserSettings.cs
/src/Configuration/SettingsProvider.cs
```

---

## Dependencies

### Internal
- Task 2.4: IContextProvider (ready)
- Task 3.2: Models.Provider (ready)

### External Libraries
- Microsoft.Extensions.Logging (ILogger)
- System.IO (File operations)
- System.Text.Json (JSON serialization)

---

## Estimated Complexity
- **ContextSizePolicy.cs**: Low (~100 lines)
- **ContextWindowManager.cs**: Medium (~250 lines)
- **UserSettings.cs**: Medium (~200 lines)
- **SettingsProvider.cs**: Medium (~150 lines)

**Total Estimated:** ~700 lines of C# code

---

## Testing Strategy

### Unit Tests
- ContextSizePolicy validation
- GetTargetSize() calculation
- Truncation algorithms
- Min/max constraint enforcement
- Settings persistence

### Integration Tests
- Integration with IContextProvider
- Integration with LLMGateway
- End-to-end context flow

---

## Known Issues

### TODO Items
1. **Model Detection:**
   - Auto-detect model from LLM response
   - Update defaults dynamically

2. **Adaptive Truncation:**
   - Learn from user preferences
   - Optimize truncation based on history

3. **Context Analytics:**
   - Track context usage patterns
   - Provide recommendations
   - Warn about inefficient usage

---

## Mockup

```
Context Settings UI:

Context Window Size: [Auto ●]
├─ Auto
├─ Fixed: [2000] tokens
└─ Percentage: [50] % of model limit

Include in Context:
☐ System prompt
☐ Working directory
☐ Privilege level

Advanced:
Min Lines: [10]
Max Lines: [500]
Min Tokens: [100]
Max Tokens: [4000]

[Reset to Defaults] [Save]
```

```
Context Manager Usage:

var settings = new ContextWindowSettings
{
    SizeMode = ContextSizeMode.Percentage,
    Percentage = 0.5,
    IncludeSystemPrompt = true,
    IncludeWorkingDirectory = true
};

var policy = new ContextSizePolicy(settings);
var manager = new ContextWindowManager(contextProvider, policy);

var result = manager.GetTruncatedContext(4000);

Console.WriteLine($"Tokens: {result.TruncatedTokens}/{result.OriginalTokens}");
Console.WriteLine($"Was Truncated: {result.WasTruncated}");
Console.WriteLine($"Reason: {result.Reason}");
```

