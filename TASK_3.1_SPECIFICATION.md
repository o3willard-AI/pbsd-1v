# Task 3.1 Specification: AI Chat UI Pane

## Task: Design and Implement AI Chat UI Pane

**Phase:** Phase 3: AI Interface
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Create the chat interface for interacting with AI assistants. This includes a message history display, input field for user messages, and a control bar for actions like sending messages, clearing history, and copying commands.

---

## Deliverables

### 1. ChatPane.xaml
Main chat pane control with:
- Scrollable message list (ItemsControl)
- Multi-line input TextBox
- Send button
- Control bar with action buttons (Clear, Copy, etc.)

### 2. ChatPane.xaml.cs
Code-behind for chat pane:
- Message collection management
- Input validation and handling
- Send command execution
- Command block detection and extraction
- Event handling for button clicks

### 3. ChatMessageControl.xaml
Individual message bubble control:
- Avatar/Icon for sender (user/AI)
- Message content area
- Timestamp display
- Copy button for commands
- Command block detection

### 4. ChatMessageControl.xaml.cs
Code-behind for message control:
- Content rendering (Markdown support)
- Syntax highlighting for code blocks
- Copy command functionality
- Expand/collapse for long messages

### 5. CommandBlockControl.xaml
Specialized control for command/code blocks:
- Dark background code block
- Syntax highlighting
- Copy button
- Language indicator

### 6. CommandBlockControl.xaml.cs
Code-behind for command block:
- Syntax highlighting logic
- Copy to clipboard
- Language detection
- Line numbers (optional)

### 7. ChatStyles.xaml
Styling resources:
- MessageBubble style (user/AI variants)
- CommandBlock style
- Input field style
- Button styles
- Scrollbar styling
- Color scheme definitions

---

## Requirements

### Functional Requirements

1. **Message Display**
   - Messages display in chronological order
   - User and AI messages visually distinguished
   - Messages scroll to bottom when new content added
   - Support for Markdown formatting (bold, italic, lists, code blocks)

2. **Input Field**
   - Multi-line text input
   - Enter sends message, Shift+Enter for new line
   - Auto-expand height based on content
   - Placeholder text

3. **Command Blocks**
   - Detect code blocks in markdown (triple backticks)
   - Display with dark background and monospace font
   - Support for language specification (e.g., ```bash)
   - Copy button for easy command execution
   - Syntax highlighting for common languages (bash, python, etc.)

4. **Control Bar**
   - Send button (disabled when input empty)
   - Clear history button
   - Copy last command button
   - Settings button (optional)

5. **Message Metadata**
   - Timestamp for each message
   - Sender identification (User/AI)
   - Token count display (optional)

### Non-Functional Requirements

1. **Performance**
   - Smooth scrolling with large message history
   - Efficient rendering of code blocks
   - Minimal UI blocking during LLM streaming

2. **Usability**
   - Keyboard shortcuts (Enter to send, Ctrl+Enter for new line)
   - Intuitive visual design
   - Accessibility support (screen readers, keyboard navigation)

3. **Responsive**
   - Resizable width
   - Adaptive layout for different window sizes
   - Proper handling of word wrapping

---

## Data Models

### ChatMessage
```csharp
public class ChatMessage
{
    public string Id { get; set; }
    public MessageSender Sender { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public int TokenCount { get; set; }
    public List<CommandBlock> CommandBlocks { get; set; }
    public bool IsStreaming { get; set; }
}

public enum MessageSender
{
    User,
    Assistant,
    System
}

public class CommandBlock
{
    public string Language { get; set; }
    public string Code { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}
```

---

## Architecture

```
ChatPane
├── MessageList (ItemsControl)
│   └── ChatMessageControl (DataTemplate)
│       ├── Avatar
│       ├── MessageContent
│       │   ├── MarkdownText
│       │   └── CommandBlockControl
│       └── Timestamp
├── InputSection
│   ├── InputTextBox
│   └── ControlBar
│       ├── SendButton
│       ├── ClearButton
│       └── CopyButton
└── ChatStyles (ResourceDictionary)
```

---

## User Flow

1. **Sending a Message**
   - User types message in input field
   - User presses Enter or clicks Send button
   - Message is added to message list
   - Message is sent to LLM Gateway
   - Response is streamed and displayed in real-time
   - Command blocks are highlighted
   - User can copy commands

2. **Copying a Command**
   - User hovers over command block
   - Copy button appears
   - User clicks Copy button
   - Command is copied to clipboard
   - Visual feedback (toast notification)

3. **Clearing History**
   - User clicks Clear button
   - Confirmation dialog appears
   - Message history is cleared
   - Session context is reset

---

## Integration Points

### Dependencies
- Task 1.3: Main window framework
- Task 3.2: LLM Gateway (for sending messages)
- Task 2.4: IContextProvider (for including terminal context)
- Task 3.4: Message history service (for persistence)

### Events
- `MessageSent`: Raised when user sends a message
- `MessageReceived`: Raised when AI response arrives
- `CommandCopied`: Raised when user copies a command
- `HistoryCleared`: Raised when history is cleared

### Services Used
- `ILLMGateway`: For sending messages to AI
- `IMessageHistory`: For persisting message history
- `IContextProvider`: For including terminal context
- `ISyntaxHighlighter`: For code block highlighting

---

## Technical Considerations

### WPF Implementation Notes
- Use `ItemsControl` with `VirtualizingStackPanel` for message list
- Implement custom `ScrollViewer` for auto-scroll behavior
- Use `FlowDocument` for rich text rendering (Markdown)
- Consider third-party libraries: Markdig (Markdown), AvalonEdit (Syntax highlighting)

### Command Detection
- Regex pattern for code blocks: ```(\w+)?\n([\s\S]*?)```
- Extract language specifier (if present)
- Parse code block content
- Create CommandBlock objects

### Streaming Support
- Support for streaming responses from LLM
- Real-time updates to message content
- Auto-scroll to show new content
- Smooth scrolling animation

### Accessibility
- ARIA labels for screen readers
- Keyboard navigation support
- High contrast mode support
- Text scaling support

---

## Acceptance Criteria

- [ ] Chat pane displays user and AI messages with different styling
- [ ] Input field accepts multi-line text with Enter to send
- [ ] Messages are displayed in chronological order
- [ ] Message list auto-scrolls to show new messages
- [ ] Command blocks are detected and displayed with dark background
- [ ] Command blocks have copy button
- [ ] Copy button copies command to clipboard
- [ ] Markdown formatting is supported (bold, italic, lists)
- [ ] Clear history button clears all messages
- [ ] Control bar has all required buttons
- [ ] Styles are defined in ChatStyles.xaml
- [ ] UI is responsive and resizable

---

## Mockup

```
┌─────────────────────────────────────────────────┐
│  AI Chat                                  [x]   │
├─────────────────────────────────────────────────┤
│                                                 │
│  [User]  How do I list files in Linux?   10:00  │
│                                                 │
│  [AI]   You can use the `ls` command to...10:01 │
│                                                 │
│          ```bash                               │
│          ls -la                                 │
│          ```                    [Copy]          │
│                                                 │
│  [User]  Can you show hidden files too?  10:02  │
│                                                 │
│  [AI]   To show hidden files...          10:03  │
│                                                 │
│          ```bash                               │
│          ls -la | grep "^\."                    │
│          ```                    [Copy]          │
│                                                 │
├─────────────────────────────────────────────────┤
│  Type your message...                          │
│                                                 │
│  [Send] [Clear] [Copy Last]                    │
└─────────────────────────────────────────────────┘
```

---

## Next Steps

After Task 3.1 is complete:
1. Task 3.2: Implement LLM Gateway Abstraction Layer
2. Task 3.3: Integrate OpenAI Provider
3. Task 3.4: Implement Message History and Syntax Highlighting

---

## Files to Create

```
/UI/Controls/ChatPane.xaml
/UI/Controls/ChatPane.xaml.cs
/UI/Controls/ChatMessageControl.xaml
/UI/Controls/ChatMessageControl.xaml.cs
/UI/Controls/CommandBlockControl.xaml
/UI/Controls/CommandBlockControl.xaml.cs
/UI/Styles/ChatStyles.xaml
```

---

## Dependencies

### Internal
- Task 1.3: Main window framework
- Task 2.4: IContextProvider (ready to use)
- PairAdmin.Models (ChatMessage model)

### External Libraries (Optional)
- Markdig: Markdown parsing
- AvalonEdit: Syntax highlighting

---

## Estimated Complexity
- **ChatPane.xaml**: Medium (~100 lines)
- **ChatPane.xaml.cs**: High (~200 lines)
- **ChatMessageControl.xaml**: Low (~50 lines)
- **ChatMessageControl.xaml.cs**: Medium (~150 lines)
- **CommandBlockControl.xaml**: Low (~50 lines)
- **CommandBlockControl.xaml.cs**: Medium (~120 lines)
- **ChatStyles.xaml**: Medium (~100 lines)

**Total Estimated:** ~770 lines of XAML/C# code

