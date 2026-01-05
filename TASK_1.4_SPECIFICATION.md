# Task 1.4: Create Embedded PuTTY Child Window Container

## Context

This task continues Phase 1: Foundation by implementing the terminal pane that will host the embedded PuTTY terminal window. This completes the window framework started in Task 1.3 by adding the actual terminal control.

### Previous Tasks Completed

- **Task 1.1:** Project structure initialized ✅
- **Task 1.2:** PuTTY source integration prepared ✅
- **Task 1.3:** Host application window framework implemented ✅

### Architecture Context

The MainWindow currently has a three-pane grid:
- **Row 0 (Terminal Pane):** Placeholder text "Terminal Pane"
- **Row 1 (Divider):** 4px draggable separator
- **Row 2 (AI Chat Pane):** Placeholder text "AI Assistant Pane"

This task will replace the Terminal Pane placeholder with a control that embeds PuTTY.

---

## Requirements

### Functional Requirements

1. **WindowsFormsHostElement for PuTTY**
   - Embed PuTTY terminal window as a WPF child control
   - Handle window resize events
   - Position and size with parent container
   - Handle keyboard focus

2. **Interop with PuTTY Library**
   - P/Invoke PuTTY functions from Task 1.2
   - Call `pairadmin_set_callback` to register for terminal events
   - Call `putty_get_terminal_hwnd` to get PuTTY window handle

3. **Window Synchronization**
   - Terminal pane resizes with MainWindow
   - Maintains minimum height constraints (200px from PRD)
   - Handles pane divider position updates

4. **Lifecycle Management**
   - Proper initialization and cleanup of PuTTY window
   - Handle window destruction safely
   - No memory leaks

### Non-Functional Requirements

1. **Performance**
   - No UI lag during window resize
   - Smooth resizing operations

2. **Robustness**
   - Handle PuTTY initialization failures gracefully
   - Recover from embedding errors
   - Provide user-friendly error messages

3. **Maintainability**
   - Clean separation between WPF UI and interop layer
   - Well-documented code
   - Following .NET naming conventions

---

## Deliverables

### Files to Create

1. **src/UI/Controls/TerminalPane.xaml**
   - WPF UserControl definition
   - WindowsFormsHost element
   - Styling (dark theme, borders)

2. **src/UI/Controls/TerminalPane.xaml.cs**
   - Code-behind for TerminalPane control
   - PuTTY window embedding logic
   - Resize event handling
   - Initialization/cleanup

3. **src/Interop/PuTTYInterop.cs**
   - C# interop layer for PuTTY C functions
   - P/Invoke declarations
   - Callback delegate definitions
   - Window handle management

### Files to Modify

4. **src/PairAdmin/MainWindow.xaml**
   - Replace Terminal Pane Border/TextBlock with TerminalPane control
   - Update grid references if needed

### Files to Update

5. **src/PairAdmin/App.xaml.cs**
   - Register PuTTYInterop as a service (if needed)
   - Add any required configuration

---

## Dependencies

### Completed Dependencies

- ✅ Task 1.1: Project structure with build system
- ✅ Task 1.2: PuTTY source integration interface (pairadmin.h, pairadmin.c)
- ✅ Task 1.3: Host application window framework

### External Dependencies

- **System.Windows.Forms** (for WindowsFormsHostElement)
- **System.Windows.Interop** (for window handle operations)
- **PuTTY static library** (PuTTY.lib or libPairAdminPuTTY.a)

---

## Acceptance Criteria

- [ ] MainWindow displays TerminalPane control instead of placeholder text
- [ ] PuTTY terminal window is embedded in the TerminalPane
- [ ] Terminal pane resizes with MainWindow and maintains minimum 200px height
- [ ] PuTTY functionality works correctly when embedded (SSH connections, keyboard input, display)
- [ ] Window handle is obtained from PuTTY via putty_get_terminal_hwnd
- [ ] Callback registration via pairadmin_set_callback succeeds (callback ready for Task 2.1)
- [ ] No memory leaks or crashes on window close
- [ ] Clean UI with dark theme styling matching PRD specifications

---

## Technical Implementation Notes

### WindowsFormsHostElement Approach

**Why:** WPF doesn't support direct HWND embedding, but can host Windows Forms controls via WindowsFormsHostElement.

**Pattern:**
```xml
<WindowsFormsHost x:Name="PuttyHost">
    <!-- PuTTY will be embedded here -->
</WindowsFormsHost>
```

### P/Invoke Declarations

**Required Functions:**
```csharp
[DllImport("PairAdminPuTTY", CallingConvention = CallingConvention.Cdecl)]
public static extern void pairadmin_set_callback(IntPtr callback);

[DllImport("PairAdminPuTTY", CallingConvention = CallingConvention.Cdecl)]
public static extern IntPtr putty_get_terminal_hwnd();
```

**Callback Delegate:**
```csharp
public delegate void PairAdminCallback(
    PairAdminEventType eventType,
    IntPtr data,
    int length);
```

### Window Handle Management

```csharp
// Get PuTTY window handle
IntPtr puttyHwnd = putty_get_terminal_hwnd();

// Assign parent to WindowsFormsHost
// This requires Win32 API calls:
SetParent(puttyHwnd, hostHandle);
SetWindowPos(puttyHwnd, hostHandle, ...);
```

### Resize Synchronization

```csharp
// In TerminalPane code-behind
protected override void OnRenderSizeChanged(SizeChangedInfo e)
{
    base.OnRenderSizeChanged(e);
    
    // Update PuTTY window size
    UpdatePuTTYWindow(this.ActualWidth, this.ActualHeight);
}

private void UpdatePuTTYWindow(double width, double height)
{
    if (_puttyHwnd != IntPtr.Zero)
    {
        SetWindowPos(_puttyHwnd, ...);
    }
}
```

---

## Code Style Guidelines

- **Naming:** PascalCase for public members, camelCase for private members
- **Comments:** XML documentation comments for all public methods
- **Null Safety:** Use nullable reference types and null-checking
- **Resource Management:** Implement IDisposable for proper cleanup
- **Async/Await:** Use async/await for any async operations

---

## Security Considerations

### Window Handle Access

- PuTTY window handle is obtained via interop
- No direct memory access to PuTTY process space
- Window handle is not exposed to AI services (filtered before callbacks)

### Process Isolation

- PuTTY runs in its own process space
- Only communication is through callbacks
- No credential exposure (as per PRD requirements)

---

## Testing Considerations

### Manual Testing Scenarios

1. **Basic Embedding**
   - Application starts without errors
   - Terminal pane is visible
   - PuTTY window appears embedded

2. **Window Resize**
   - Resizing main window resizes terminal pane
   - Terminal pane maintains minimum 200px height
   - No visual glitches during resize

3. **PuTTY Functionality**
   - SSH connections can be established
   - Keyboard input works in terminal
   - Terminal output displays correctly
   - ANSI escape sequences render properly

4. **Window Lifecycle**
   - Closing main window cleans up PuTTY
   - No crashes on close
   - No zombie processes left running

5. **Error Handling**
   - PuTTY library not found: Show user-friendly error
   - Window embedding fails: Show user-friendly error
   - Initialization timeout: Show user-friendly error

### Future Automated Tests

These will be added in later phases:
- Unit tests for interop layer
- Integration tests with mock PuTTY
- UI automation tests for window management

---

## Implementation Priority

### High Priority (Must Have)

1. Basic PuTTY embedding with WindowsFormsHostElement
2. Window handle management and parent assignment
3. Resize synchronization
4. Lifecycle management (cleanup)

### Medium Priority (Should Have)

1. Error handling and user-friendly messages
2. Configuration for PuTTY options (if any)
3. Keyboard focus management

### Low Priority (Nice to Have)

1. Debug/logging for interop operations
2. Performance monitoring
3. Telemetry for embedding operations

---

## Known Limitations

1. **Linux/macOS Development**
   - WindowsFormsHostElement is Windows-specific
   - For cross-platform development, use placeholder/stub
   - Actual embedding requires Windows environment

2. **PuTTY Library Dependency**
   - Depends on PuTTY.lib being built and linked
   - Currently using stub interface (Task 1.2)
   - Actual PuTTY integration required for production

3. **Window Handle Lifecycle**
   - PuTTY window handle lifecycle is complex
   - Must ensure proper cleanup to avoid zombie windows

---

## Next Steps

After Task 1.4 completion:

1. **Phase 1 Complete** - All foundation tasks done
2. **Begin Phase 2:** I/O Interception
   - Task 2.1: PuTTY source modifications
   - Task 2.2: Build I/O Interceptor module
   - Task 2.3: Implement circular buffer
   - Task 2.4: Create context manager

3. **Integration Testing**
   - Test embedded PuTTY with actual SSH connections
   - Verify callbacks fire correctly
   - Measure performance overhead

---

## References

- **PRD:** /home/sblanken/Downloads/PairAdmin_PRD_v1.0.md
- **Implementation Plan:** /home/sblanken/working/bsd/PairAdmin_Implementation_Plan.md
- **Task 1.2 Files:** /home/sblanken/working/bsd/PairAdmin/src/PuTTY/
- **Task 1.3 Files:** /home/sblanken/working/bsd/PairAdmin/src/PairAdmin/
- **PuTTY Integration:** /home/sblanken/working/bsd/PairAdmin/src/PuTTY/README_INTEGRATION.md

---

**Task Status:** Ready for ALCS implementation
**Estimated Complexity:** Medium
**Estimated Time:** 1-2 hours with ALCS Alpha-Beta iteration
