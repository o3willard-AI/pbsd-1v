# Task 2.2: Build I/O Interceptor Module to Capture Terminal Streams

## Context

This task continues Phase 2: I/O Interception by implementing the module that will receive and process terminal I/O events from PuTTY's callback hooks.

### Previous Tasks Completed

- **Task 2.1:** Implement PuTTY source modifications for callback hooks ✅
- **Task 1.1-1.4:** Phase 1: Foundation ✅

---

## Requirements

### Functional Requirements

1. **Callback Registration System**
   - Receive PuTTY callback events (OUTPUT, INPUT) via PuTTYInterop
   - Manage callback delegate lifecycle
   - Register/unregister callbacks dynamically
   - Thread-safe callback handling

2. **Event Management**
   - Define terminal I/O event types (Output, Input, Error)
   - Create event args classes for each type
   - Support event subscription/unsubscription
   - Event marshaling from C# to C# callers

3. **Terminal Output Capture**
   - Capture terminal output events from PuTTY
   - Parse raw byte data to strings
   - Detect ANSI escape sequences (optional)
   - Track output statistics (lines, bytes, timestamps)

4. **Terminal Input Capture**
   - Capture user input events from PuTTY
   - Log commands before transmission
   - Detect command patterns
   - Track input statistics

5. **Data Filtering (Pre-Security)**
   - Filter sensitive data before processing (password prompts, API keys)
   - Mask private key blocks
   - Redact sensitive patterns
   - Log filtered data (debug level only)

### Non-Functional Requirements

1. **Performance**
   - Minimal overhead for event processing (<1ms per event)
   - No blocking operations in callback handlers
   - Efficient string handling and byte-to-string conversion
   - Buffer management to reduce allocations

2. **Robustness**
   - Handle null/invalid callbacks gracefully
   - Recover from marshaling errors
   - Thread-safe event dispatching
   - Proper error handling and logging
   - Resource cleanup (IDisposable pattern)

3. **Maintainability**
   - Clean separation of concerns (events, parsing, filtering, statistics)
   - Well-documented public APIs
   - XML documentation comments
- - Unit test coverage (>90%)
- - Follows .NET naming conventions
- - Dependency injection support

4. **Thread Safety**
   - All shared state protected by locks or ConcurrentDictionary
   - Event dispatch is thread-safe
   - Callback registration is atomic
   - Statistics counters use thread-safe operations

---

## Deliverables

### Files to Create

1. **src/IoInterceptor/IOInterceptor.cs** (Main module)
   - IOInterceptor class for managing callback registration
   - Event definitions and event args classes
   - Public API for event subscription

2. **src/IoInterceptor/TerminalOutputCapture.cs**
   - Capture terminal output from PuTTY callbacks
   - Raw data to string conversion
   - ANSI escape sequence detection (basic)
   - Output filtering and sanitization

3. **src/IoInterceptor/TerminalInputCapture.cs**
   - Capture terminal input from PuTTY callbacks
   - Command parsing and validation
   - Input logging for audit trail

4. **src/IoInterceptor/OutputEventHandler.cs**
   - Event handler for terminal output events
   - Event marshaling and dispatching
   - Statistics tracking

5. **src/IoInterceptor/InputEventHandler.cs**
   - Event handler for terminal input events
   - Event marshaling and dispatching
   - Statistics tracking

6. **src/IoInterceptor/Events/TerminalOutputEventArgs.cs**
   - Event args for terminal output events
   - Contains raw data, parsed string, timestamp

7. **src/IoInterceptor/Events/TerminalInputEventArgs.cs**
   - Event args for terminal input events
   - Contains command string, timestamp, byte data

8. **src/IoInterceptor/Models/TerminalStatistics.cs**
   - Statistics model for terminal I/O
   - Counts for output/input events, bytes, lines
   - Session start time, last activity time

9. **src/IoInterceptor/Services/DataFilterService.cs**
   - Service for filtering sensitive data
   - Pattern definitions (passwords, keys, tokens)
   - Filtering logic (exclude, replace, mask)
   - Configurable patterns via settings

10. **src/IoInterceptor/IoInterceptorConfiguration.cs**
   - Configuration settings
   - Enable/disable filtering
   - Filter patterns list
   - Logging levels
   - Event buffering settings

11. **src/IoInterceptor/Tests/OutputCaptureTests.cs**
   - Unit tests for output capture
   - Event handler tests
   - Data filtering tests
   - Statistics tracking tests

### Files to Modify

1. **src/IoInterceptor/IoInterceptor.csproj**
   - Reference PuTTYInterop project
   - Add test dependencies (xUnit, Moq, FluentAssertions)

---

## Dependencies

### Completed Dependencies

- ✅ Task 2.1: PuTTY source modifications (callback interface defined)
- ✅ Task 1.1: Project structure (IoInterceptor project created)
- ✅ Task 1.2: PuTTY integration (pairadmin.h, pairadmin.c in Interop project)
- ✅ DataStructures project (for reusable data structures)
- ✅ Logging infrastructure (Microsoft.Extensions.Logging)

### External Dependencies

- System.Reactive (4.0.1) - For reactive event streams
- System.Text.RegularExpressions - For pattern matching
- Microsoft.Extensions.Options.Abstractions (via DataStructures)
- Microsoft.Extensions.Logging.Abstractions (via DataStructures)

---

## Architecture

### Event Flow

```
PuTTY Terminal
    ↓
    Callbacks (via PuTTYInterop)
    ↓
IOInterceptor Module
    ├─→ Callback Registration Manager
    ├─→ Output Event Handler
    ├─→ Input Event Handler
    └─→ Event Dispatch System
         ↓
    Subscribers (Context Manager, Security, etc.)
```

### Class Organization

```
IoInterceptor/
├── IOInterceptor.cs              # Main module entry point
├── TerminalOutputCapture.cs    # Output capture and processing
├── TerminalInputCapture.cs     # Input capture and processing
├── OutputEventHandler.cs        # Output event handling
├── InputEventHandler.cs         # Input event handling
├── Events/                    # Event args definitions
│   ├── TerminalOutputEventArgs.cs
│   └── TerminalInputEventArgs.cs
├── Models/
│   └── TerminalStatistics.cs     # I/O statistics
├── Services/
│   ├── DataFilterService.cs       # Sensitive data filtering
│   └── IoInterceptorConfiguration.cs  # Configuration management
└── Tests/                       # Unit tests
    └── OutputCaptureTests.cs
```

---

## Implementation Notes

### Event System Design

**Why System.Reactive?**
- Natural fit for event-driven architecture
- Supports multiple subscribers efficiently
- Thread-safe event dispatching
- Built-in operators (Where, Select, etc.)
- Easy to test and debug

**Event Types:**
```csharp
public enum TerminalIOEventType : byte
{
    Output = 1,
    Input = 2,
    Error = 3
}
```

**Event Args Interface:**
```csharp
public interface ITerminalIOEventArgs
{
    DateTime Timestamp { get; }
    byte[] RawData { get; }
    string ParsedData { get; }
}
```

### Callback Registration

**Pattern:**
```csharp
public void RegisterCallback(PairAdminCallback callback)
{
    // Store callback delegate
    // Subscribe to internal events
    // Forward PuTTY callbacks to event system
    
    // Thread safety:
    // - Store delegate in thread-safe manner
    // - Use lock or Interlocked for delegate invocation
}

public void UnregisterCallback()
{
    // Remove callback delegate
    // Stop forwarding PuTTY callbacks
    // Cleanup internal state
}
```

### Data Filtering Strategy

**Before Processing:**
```csharp
if (dataFilterService.IsSensitive(outputData))
{
    // Log and skip
    logger.LogDebug("Filtered sensitive data: {Truncated}");
    return;
}
```

**Filter Categories:**
1. **Password Prompts:** Lines containing "password:", "passphrase:", "Enter password:"
2. **Private Keys:** Lines containing "-----BEGIN.*PRIVATE KEY-----"
3. **API Keys:** Patterns like "sk-[a-zA-Z0-9]{48}"
4. **Custom Patterns:** User-configurable regex patterns

### Statistics Tracking

**Metrics:**
- Output events count
- Input events count
- Total output bytes
- Total input bytes
- Lines of output
- Session duration
- Last activity timestamp

**Usage:**
- Performance monitoring
- Debugging aid
- Audit trail preparation
- Usage analytics

---

## Acceptance Criteria

- [ ] IOInterceptor.cs can be instantiated with ILogger
- [ ] RegisterCallback() successfully registers PuTTY callbacks
- [ ] TerminalOutputCapture captures output events correctly
- [ ] TerminalInputCapture captures input events correctly
- [ ] Events are dispatched to subscribers
- [ ] DataFilterService filters sensitive data correctly
- [ ] TerminalStatistics track I/O metrics accurately
- [ ] All classes have XML documentation comments
- [ ] Unit tests pass with >90% coverage
- [ ] No blocking operations in event handlers
- [ ] Thread-safe event dispatching verified
- [ ] Memory usage is efficient (no leaks)
- [ ] Events can be subscribed/unsubscribed
- [ ] Configuration can be loaded and modified
- [ ] Integration with PuTTYInterop works correctly

---

## Code Style Guidelines

- **Naming:** PascalCase for classes, camelCase for members
- **Interfaces:** Prefix with `I` (ITerminalIOEventArgs)
- **Events:** Suffix with `EventArgs`
- **Services:** Suffix with `Service`
- **Models:** Suffix with `Model`
- **Exceptions:** Suffix with `Exception`

- **Async/Await:**
  - Use async/await for I/O operations
  - Return Task<T> or ValueTask<T> for async operations
  - Configure async state machine (if needed)

- **Null Safety:**
  - Use nullable reference types (string?, byte[])
  - Null-check callback parameters
  - Use null-conditional operators (??, ?.?)

- **Resource Management:**
  - Implement IDisposable for classes holding resources
  - Use using statements for IDisposable objects
  - Call Dispose() in cleanup code

- **Logging:**
  - Use ILogger<T> for typed logging
  - Log at appropriate levels (Debug, Info, Warning, Error)
  - Include context information (event type, data length)
  - Avoid logging sensitive data

- **Comments:**
  - XML documentation comments for all public methods
  - Explain non-obvious logic
  - Document threading and synchronization
  - Reference requirements by line number

---

## Security Considerations

### Data Privacy

- **Never Log Sensitive Data:** Filter before logging (debug level only)
- **Never Store Sensitive Data:** Exclude from statistics
- **Never Transmit Sensitive Data:** Don't send to AI/LLM

### Input Validation

- **Validate Callback Parameters:** Check for null/invalid callbacks
- **Sanitize Input Data:** Remove ANSI escape sequences from input
- **Length Limits:** Maximum event data size (configurable)

### Threat Mitigation

- **Code Injection:** Validate all event data before processing
- **Injection in Logs:** Escape special characters in log messages
- **Memory Safety:** Validate array bounds before access
- **Buffer Overflow:** Use fixed-size buffers or length checks

---

## Performance Considerations

### Optimization Targets

- **Event Dispatch:** <1ms from callback to subscriber
- **Event Processing:** <10ms per event
- **String Conversion:** <5ms for byte-to-string conversion
- **Statistics Update:** <1ms per counter increment
- **Memory Allocation:** Minimize allocations in hot path

### Strategies

1. **Object Pooling:** Reuse event args objects
2. **Array Pooling:** For byte buffers
3. **String Interning:** For frequently used strings
4. **Lazy Evaluation:** Defer heavy operations until needed
5. **Async Processing:** Use async/await for I/O operations

---

## Testing Strategy

### Unit Tests

**Test Coverage Target:** >90%

**Test Categories:**
1. **Callback Registration Tests**
   - Register callback with valid delegate
   - Register null callback (should handle gracefully)
   - Unregister callback
   - Thread safety of registration

2. **Event Handling Tests**
   - Event dispatching
   - Subscriber notification
   - Multiple subscribers receive events
   - Event args are passed correctly
   - Timestamp accuracy

3. **Output Capture Tests**
   - Raw data processing
   - String conversion
   - ANSI detection
   - Statistics counting
   - Data filtering

4. **Input Capture Tests**
   - Command parsing
   - Command validation
   - Command logging
   - Statistics tracking

5. **Data Filtering Tests**
   - Password prompt detection
   - Private key detection
   - API key detection
   - Custom pattern matching
   - Replace/mask operations

6. **Integration Tests**
   - Mock PuTTYInterop callbacks
   - Test event flow
   - Test error handling
   - Test resource cleanup

### Mock Strategy

**PuTTYInterop Mock:**
```csharp
var mockInterop = new Mock<IPuTTYInterop>();
mockInterop.Setup(registerCallback: (callback) => {});
```

---

## Integration Points

### Downstream Dependencies

**Task 2.3: Circular Buffer** (needs terminal output events)
- Will store terminal output captured by this module
- Depends on TerminalOutputCapture

**Task 2.4: Context Manager** (needs terminal output)
- Will extract context from buffered output
- Depends on TerminalOutputCapture and CircularBuffer

**Task 7.1: Security Module** (needs terminal I/O)
- Will use I/O events for command validation
- Depends on this module

---

## Configuration Settings

### Default Settings (IoInterceptorConfiguration.cs)

```json
{
  "DataFiltering": {
    "Enabled": true,
    "FilterPasswords": true,
    "FilterPrivateKeys": true,
    "FilterAPIKeys": true,
    "CustomPatterns": []
  },
  "EventBuffering": {
    "MaxEventsInMemory": 1000,
    "BufferSize": 8192
  },
  "Statistics": {
    "TrackOutputEvents": true,
    "TrackInputEvents": true,
    "TrackBytes": true,
    "TrackLines": true
  },
  "Logging": {
    "Level": "Information",
    "LogFilteredData": false
  }
}
```

---

## Technical Specifications

### Terminal Output Capture

**Data Types:**
```csharp
public class TerminalOutputCapture
{
    public IObservable<TerminalOutputEventArgs> OutputEvents { get; }
    
    private void ProcessOutput(byte[] data, int length)
    {
        // Convert bytes to string
        string output = Encoding.UTF8.GetString(data, 0, length);
        
        // Filter sensitive data
        if (_dataFilterService.IsSensitive(output))
        {
            _logger.LogDebug("Filtered sensitive output");
            return;
        }
        
        // Detect ANSI sequences (basic)
        bool isANSI = _ansiDetector.DetectEscapeSequences(output);
        
        // Create event args
        var args = new TerminalOutputEventArgs
        {
            Timestamp = DateTime.UtcNow,
            RawData = (byte[])data.Clone(),
            ParsedData = output
        };
        
        // Publish event
        _outputEvents.OnNext(args);
        
        // Update statistics
        _statistics.IncrementOutputCount();
        _statistics.AddOutputBytes(length);
    }
}
```

### Terminal Input Capture

**Data Types:**
```csharp
public class TerminalInputCapture
{
    public IObservable<TerminalInputEventArgs> InputEvents { get; }
    
    private void ProcessInput(byte[] data, int length)
    {
        // Convert bytes to string
        string input = Encoding.UTF8.GetString(data, 0, length);
        
        // Sanitize ANSI escape sequences
        string sanitized = _ansiStripper.StripANSICodes(input);
        
        // Validate command syntax (basic)
        bool isValid = _commandValidator.ValidateSyntax(sanitized);
        
        // Create event args
        var args = new TerminalInputEventArgs
        {
            Timestamp = DateTime.UtcNow,
            RawData = (byte[])data.Clone(),
            CommandString = sanitized,
            IsValid = isValid
        };
        
        // Publish event
        _inputEvents.OnNext(args);
        
        // Update statistics
        _statistics.IncrementInputCount();
        _statistics.AddInputBytes(length);
    }
}
```

---

## Error Handling

### Error Scenarios

1. **PuTTYInterop Errors**
   - Callback registration fails
   - Invalid callback pointer
   - Win32 API failures

2. **Marshaling Errors**
   - Invalid UTF-8 encoding
   - Buffer overflow
   - String conversion failures

3. **Event Processing Errors**
   - Invalid event data
   - Null callback delegate
   - Subscriber errors

**Recovery Strategy:**
```csharp
try {
    // Operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in {OperationName}");
    // Recover or fail gracefully
}
```

---

## Future Enhancements

### Phase 2+ (Beyond Current Scope)

1. **Advanced ANSI Processing**
   - Full terminal emulation
   - Screen content queries
   - Cursor position tracking

2. **Command Intelligence**
   - Command pattern recognition
   - Command prediction
   - Auto-completion suggestions

3. **Analytics Integration**
   - Real-time I/O analytics
   - Performance metrics dashboard
   - Usage pattern analysis

4. **Replay Capabilities**
   - Terminal session replay
   - I/O event timeline
   - Debugging support

---

## Documentation Requirements

### XML Documentation Comments

**Required for all public members:**
```csharp
/// <summary>
/// Brief description of member's purpose
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value and type</returns>
/// <exception cref="ExceptionType">Exceptions that can be thrown</exception>
```

### Class-Level Documentation

Each class should have:
- Purpose statement (what this class does)
- Thread safety notes
- Usage examples
- Dependencies list
- Performance characteristics
- Known limitations

---

## Build Verification

### Compiler Warnings

**Target:** Zero warnings, zero errors

**Common Issues to Avoid:**
- CS0168: Unassigned local variables
- CS0216: Warning - unreachable code detected
- CS1591: Missing XML comment for publicly visible type/member
- CS3005: Warning - unassigned field accessed
- CS8618: Warning - null reference type is non-nullable

### Static Analysis

**Target:** No high-severity warnings

**Tools to Use:**
- Roslyn analyzers
- StyleCop rules (if configured)
- CodeQL (if available)

---

## Success Metrics

### Code Metrics

- **Estimated Lines of Code:** ~2,000 lines
- **Number of Classes:** 12-15
- **Number of Interfaces:** 2-5
- **Number of Event Types:** 3
- **Test Classes:** 10-15
- **Files Created:** 11 files

### Completeness Metrics

| Category | Target | Notes |
|----------|--------|-------|
| **Core Functionality** | 100% | All requirements implemented |
| **Event System** | 100% | Reactive event dispatch |
| **Data Filtering** | 100% | Sensitive data filters |
| **Statistics** | 100% | I/O metrics tracking |
| **Testing** | 90%+ | Unit test coverage |
| **Documentation** | 100% | All public APIs documented |
| **Thread Safety** | 100% | Concurrent access protected |
| **Error Handling** | 100% | Comprehensive error handling |

---

## Notes

### Implementation Strategy

**Modular Design:**
- Each class has single responsibility
- Events defined separately from handlers
- Statistics isolated in its own model
- Data filtering is its own service
- Easy to test and maintain

**Dependency Injection:**
- All services receive ILogger via constructor
- Configuration injectable via IOptions pattern
- Services can be easily mocked for testing

**Extensibility:**
- Event-based architecture allows easy addition of new subscribers
- Configuration system allows runtime adjustments
- Filter patterns can be added without code changes

---

## Next Steps

**After Task 2.2 Completion:**

1. **Task 2.3:** Implement Circular Buffer for Terminal Output Storage
   - Depends on TerminalOutputCapture events
   - Implements in-memory circular buffer
   - Configurable buffer size
   - Sliding window extraction

2. **Task 2.4:** Create Context Manager Interface with Sliding Window Support
   - Depends on CircularBuffer
   - Extracts context from buffered terminal output
   - Provides context to LLM gateway
   - Manages token counting

---

## Estimated Complexity

| Component | Complexity | Estimated Time |
|-----------|-----------|--------------|
| IOInterceptor Module | Medium | 3-4 hours |
| TerminalOutputCapture | Medium | 2-3 hours |
| TerminalInputCapture | Medium | 2-3 hours |
| Event Handlers | Low | 2-3 hours |
| Statistics Model | Low | 1-2 hours |
| Data Filter Service | Medium | 2-3 hours |
| Configuration | Low | 1-2 hours |
| Unit Tests | Medium | 3-4 hours |

**Total:** 16-26 hours

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|-------|-------------|---------|-------------|
| Event dispatch performance issues | Low | High | Use System.Reactive, implement batching |
| Memory leaks from events | Low | High | Use IDisposable, test with dotMemory |
| Thread safety violations | Low | High | Use Interlocked/ReaderWriterLock |
| Data filtering bypass | Medium | High | Logging, regular audits |
| Callback timing issues | Low | Medium | Document expected behavior |

---

## References

- **PairAdmin PRD:** /home/sblanken/Downloads/PairAdmin_PRD_v1.0.md
- **Implementation Plan:** /home/sblanken/working/bsd/PairAdmin/PairAdmin_Implementation_Plan.md
- **Task 2.1 Report:** /home/sblanken/working/bsd/PairAdmin/TASK_2.1_COMPLETION_REPORT.md
- **PuTTY Integration Docs:** /home/sblanken/working/bsd/PairAdmin/src/PuTTY/README_INTEGRATION.md

**Dependencies:**
- PuTTY Interop: /home/sblanken/working/bsd/PairAdmin/src/Interop/PuTTYInterop.cs
- DataStructures: /home/sblanken/working/bsd/PairAdmin/src/DataStructures/

---

**Task Status:** Ready for Implementation

**Phase 2.1 Status:** ✅ COMPLETE
**Task 2.2 Status:** IN PROGRESS

---

**Estimated Completion:** 16-26 hours from start

**Note:** This is a comprehensive specification. Implementation will follow these guidelines and requirements closely.
