# Task 4.2 Specification: Token Counting and Context Meter UI Component

## Task: Add Token Counting and Context Meter UI Component

**Phase:** Phase 4: Context Awareness
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Implement token counting service and UI component for displaying context usage with a color-coded progress bar. The meter shows real-time token usage as a percentage of the model's context window, changing colors at warning thresholds.

---

## Deliverables

### 1. TokenCounter.cs
Token counting service:
- Accurate token counting for various models
- Support for multiple tokenizers
- Context-aware counting
- Usage tracking

### 2. ContextMeter.xaml
WPF control for displaying context usage:
- Progress bar (green/yellow/red)
- Token count display
- Percentage display
- Visual feedback

### 3. ContextMeter.xaml.cs
Code-behind for ContextMeter:
- Progress bar logic
- Color threshold management
- Event handling
- Update methods

---

## Requirements

### Functional Requirements

1. **Token Counting**
   - Support for different tokenizers (GPT, Claude, etc.)
   - Accurate counting for common languages
   - Context-aware counting
   - Usage tracking across sessions

2. **Context Meter Display**
   - Show current token count
   - Show percentage of context window
   - Show maximum token limit
   - Real-time updates

3. **Color Coding**
   - Green (< 70%): Safe usage
   - Yellow (70-90%): Warning
   - Red (> 90%): Critical
   - Smooth color transitions

4. **User Feedback**
   - Warning when approaching limit
   - Alert when exceeding limit
   - Tooltip with detailed info
   - Accessibility support

### Non-Functional Requirements

1. **Performance**
   - Efficient token counting
   - Fast UI updates
   - Minimal overhead

2. **Usability**
   - Clear visual feedback
   - Intuitive color coding
   - Responsive to window resize

3. **Reliability**
   - Accurate token counts
   - Consistent color thresholds
   - Stable updates

---

## Data Models

### TokenizerType
```csharp
public enum TokenizerType
{
    GPT35,        // GPT-3.5 tokenizer (~4 chars/token)
    GPT4,         // GPT-4 tokenizer
    Claude,        // Claude tokenizer (~4 chars/token)
    Local,         // Local models (qwen, deepseek, etc.)
    Heuristic      // Fallback (~4 chars/token)
}
```

### TokenCountResult
```csharp
public class TokenCountResult
{
    public int TotalTokens { get; set; }
    public int ContextTokens { get; set; }
    public int SystemTokens { get; set; }
    public double Percentage { get; set; }
    public TokenizerType Tokenizer { get; set; }
}
```

### ContextMeterState
```csharp
public class ContextMeterState
{
    public int CurrentTokens { get; set; }
    public int MaxTokens { get; set; }
    public double Percentage { get; set; }
    public MeterStatus Status { get; set; }
}

public enum MeterStatus
{
    Safe,       // Green (< 70%)
    Warning,    // Yellow (70-90%)
    Critical    // Red (> 90%)
}
```

---

## Architecture

```
Token Counting System
┌─────────────────────────────────────┐
│       TokenCounter (Service)       │
│  - Tokenizer Selection           │
│  - Accurate Counting            │
│  - Usage Tracking               │
└──────────┬────────────────────────┘
           │ TokenCountResult
           ▼
┌─────────────────────────────────────┐
│      ContextMeter (UI Control)     │
│  - Progress Bar                │
│  - Color Coding                 │
│  - Token Count Display          │
│  - Percentage Display           │
└──────────┬────────────────────────┘
           │ Update Events
           ▼
┌─────────────────────────────────────┐
│         ChatPane (from 3.1)       │
│  - Real-time Updates             │
└─────────────────────────────────────┘
```

### Data Flow

```
User types message → ChatPane
    ↓ EstimateTokenCount()
    ↓ TokenCounter.CountTokens()
    ↓ Update ContextMeter
    ↓ Update Progress Bar (color-coded)
    ↓ Display Current Tokens / Max Tokens
    ↓ Display Percentage (e.g., "75%")
```

---

## Token Counting Algorithms

### GPT-3.5 Tokenizer
```
Approximation: ~4 characters per token
Formula: ceiling(text.length / 4)
Accuracy: ~95% for typical text
```

### GPT-4 Tokenizer
```
Approximation: ~3.5 characters per token
Formula: ceiling(text.length / 3.5)
Accuracy: ~90% for typical text
```

### Claude Tokenizer
```
Approximation: ~4 characters per token
Formula: ceiling(text.length / 4)
Accuracy: ~95% for typical text
```

### Local Models (Qwen, Deepseek)
```
Approximation: ~3.5-4 characters per token
Formula: ceiling(text.length / 4)
Accuracy: ~85-90% for typical text
```

### Fallback Heuristic
```
Approximation: ~4 characters per token
Formula: ceiling(text.length / 4)
Use when tokenizer not available
```

---

## Color Thresholds

| Percentage | Color | Status |
|------------|-------|--------|
| 0% - 70% | Green | Safe |
| 70% - 90% | Yellow | Warning |
| 90% - 100% | Red | Critical |
| > 100% | Red | Over limit |

### Color Palette
```xml
<SolidColorBrush x:Key="MeterSafeColor" Color="#2ECC71" />      <!-- Green -->
<SolidColorBrush x:Key="MeterWarningColor" Color="#F1C40F" />  <!-- Yellow -->
<SolidColorBrush x:Key="MeterCriticalColor" Color="#E74C3C" /> <!-- Red -->
<SolidColorBrush x:Key="MeterOverlimitColor" Color="#C0392B" /> <!-- Dark Red -->
```

---

## Integration Points

### Dependencies
- Task 4.1: ContextWindowManager ✅
- Task 3.1: ChatPane ✅
- Task 3.2: LLMGateway (for model info) ✅

### Integration with ChatPane
```csharp
var tokenCounter = new TokenCounter();
var contextMeter = new ContextMeter();

// Subscribe to events
chatPane.MessageSent += (s, e) =>
{
    var tokens = tokenCounter.CountTokens(e.Message.Content);
    contextMeter.UpdateTokens(tokens.Current, maxTokens);
};

chatPane.StreamingChunkReceived += (s, e) =>
{
    var tokens = tokenCounter.CountTokens(e.Content);
    contextMeter.UpdateTokens(tokens.Current, maxTokens);
};
```

### Integration with ContextWindowManager
```csharp
var maxTokens = contextWindowManager.GetEffectiveMax();
var currentTokens = tokenCounter.CountTokens(currentContext);

var result = new TokenCountResult
{
    CurrentTokens = currentTokens,
    MaxTokens = maxTokens,
    Percentage = (double)currentTokens / maxTokens
};

contextMeter.Update(result);
```

---

## Technical Considerations

### Token Counting Performance
- Cache token counts for repeated text
- Use efficient string operations
- Avoid unnecessary allocations
- Parallelize large token counts

### UI Performance
- Use INotifyPropertyChanged for updates
- Update progress bar width (not rebuild)
- Smooth color transitions
- Debounce rapid updates

### Color Transitions
- Linear interpolation between colors
- Smooth transitions at thresholds
- Visual feedback on state change

### Accessibility
- ARIA labels for progress bar
- Keyboard navigation support
- High contrast mode
- Screen reader announcements

---

## Acceptance Criteria

- [ ] Token count is accurate (within 10%)
- [ ] Meter updates in real-time
- [ ] Colors change at 70%, 90% thresholds
- [ ] Progress bar shows correct percentage
- [ ] Token count is displayed
- [ ] Maximum tokens is displayed
- [ ] Tooltip shows detailed information
- [ ] Updates are smooth (no flickering)
- [ ] Integration with ChatPane works
- [ ] Integration with ContextWindowManager works
- [ ] Supports multiple tokenizers
- [ ] Fallback heuristic works

---

## Next Steps

After Task 4.2 is complete:
1. Task 4.3: Parse and Track Current Working Directory
2. Task 4.4: Detect and Track User Privilege Levels

---

## Files to Create

```
/src/LLMGateway/TokenCounter.cs
/src/UI/Controls/ContextMeter.xaml
/src/UI/Controls/ContextMeter.xaml.cs
```

---

## Dependencies

### Internal
- Task 3.1: ChatPane (events) ✅
- Task 4.1: ContextWindowManager (limits) ✅

### External Libraries
- Microsoft.Extensions.Logging (ILogger)
- System.Collections.Concurrent (for caching)

---

## Estimated Complexity
- **TokenCounter.cs**: Medium (~250 lines)
- **ContextMeter.xaml**: Low (~80 lines)
- **ContextMeter.xaml.cs**: Medium (~200 lines)

**Total Estimated:** ~530 lines of code

---

## Testing Strategy

### Unit Tests
- TokenCounter accuracy for each tokenizer
- TokenCountResult validation
- Percentage calculation
- Color threshold logic

### Integration Tests
- Real-time updates from ChatPane
- Integration with ContextWindowManager
- End-to-end token flow

---

## Known Issues

### TODO Items
1. **Precise Tokenizing:**
   - Integrate actual tokenizers (tiktoken, claudetk)
   - Provider-specific token counts
   - Model-specific adjustments

2. **History Tracking:**
   - Track token usage over time
   - Usage analytics
   - Cost tracking

3. **Adaptive Thresholds:**
   - User-configurable thresholds
   - Learn from usage patterns
   - Dynamic adjustment

---

## Mockup

```
Context Meter UI:

┌──────────────────────────────────────────┐
│ Context Usage                          │
│ ┌────────────────────────────────────┐ │
│ │ Tokens: 3,500 / 4,096         │ │
│ │ Progress Bar                    │ │
│ │ ░░░░░░░░░░░░░░░░░░░░░░░░░░  │ │
│ │ [████████████░░░░░░░░░░░] 85%   │ │
│ │                                  │ │
│ │ ▲ Token Count Tooltip             │ │
│ │   Current: 3,500 tokens        │ │
│ │   Max: 4,096 tokens           │ │
│ │   Available: 596 tokens (15%)   │ │
│ │   Status: Warning               │ │
│ └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

Color legend:
- [████████████░░░░░░░░░░░░] Green (< 70%)
- [████████████████░░░░░░░░] Yellow (70-90%)
- [██████████████████████████] Red (> 90%)

```
Context Meter Usage:

var tokenCounter = new TokenCounter(tokenizerType: TokenizerType.GPT35);
var contextMeter = new ContextMeter();

// Update from ChatPane
chatPane.StreamingChunkReceived += (s, e) =>
{
    var currentTokens = tokenCounter.CountTokens(context);
    var maxTokens = contextWindowManager.GetEffectiveMax();
    var percentage = (double)currentTokens / maxTokens;

    contextMeter.Update(
        currentTokens: currentTokens,
        maxTokens: maxTokens,
        percentage: percentage
    );
};
```

