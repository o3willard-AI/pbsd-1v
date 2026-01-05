# Task 3.2 Specification: LLM Gateway Abstraction Layer

## Task: Implement LLM Gateway Abstraction Layer

**Phase:** Phase 3: AI Interface
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Create a provider-agnostic interface for LLM (Large Language Model) communication. The gateway will abstract away provider-specific details, allowing the application to work with different LLM providers (OpenAI, Anthropic, Google, Ollama, etc.) through a unified interface.

---

## Deliverables

### 1. ILLMProvider.cs
Interface for LLM providers:
- Async completion methods
- Streaming support
- Error handling
- Token counting

### 2. LLMGateway.cs
Main gateway class:
- Provider registration system
- Active provider management
- Request routing
- Context integration
- Event handling

### 3. ProviderConfiguration.cs
Configuration for LLM providers:
- API keys
- Model selection
- Parameters (temperature, max tokens, etc.)
- Endpoint URLs

### 4. CompletionRequest.cs
Request model for completions:
- Messages
- Model parameters
- Context (terminal output)
- Streaming flag

### 5. CompletionResponse.cs
Response model for completions:
- Generated text
- Token usage
- Finish reason
- Provider metadata

### 6. Models/Provider.cs
Provider metadata and information:
- Provider name
- Supported models
- Endpoint
- Features

### 7. Models/Message.cs
Message model for LLM API:
- Role (system, user, assistant)
- Content
- Timestamp

---

## Requirements

### Functional Requirements

1. **Provider Abstraction**
   - Unified interface for all LLM providers
   - Support for multiple providers
   - Provider registration and selection
   - Dynamic provider switching

2. **Completion Methods**
   - Non-completion (blocking) API calls
   - Streaming completions
   - Async/await support
   - Cancellation tokens

3. **Context Integration**
   - Include terminal context from IContextProvider
   - Configurable context window size
   - Context formatting for LLM prompt

4. **Error Handling**
   - Provider-specific exceptions
   - Retry logic with exponential backoff
   - Graceful degradation
   - Error reporting

5. **Token Management**
   - Token counting
   - Usage tracking
   - Token limits
   - Context truncation

### Non-Functional Requirements

1. **Performance**
   - Efficient request handling
   - Minimal overhead for abstraction
   - Connection pooling
   - Async operations

2. **Extensibility**
   - Easy to add new providers
   - Plugin architecture
   - Provider discovery

3. **Reliability**
   - Connection error recovery
   - Timeout handling
   - Request queuing
   - Rate limiting

---

## Data Models

### Message
```csharp
public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime? Timestamp { get; set; }
}

public enum MessageRole
{
    System,
    User,
    Assistant
}
```

### CompletionRequest
```csharp
public class CompletionRequest
{
    public List<Message> Messages { get; set; }
    public string Model { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public bool Stream { get; set; }
    public string? Context { get; set; }
    public CancellationToken CancellationToken { get; set; }
}
```

### CompletionResponse
```csharp
public class CompletionResponse
{
    public string Content { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public string FinishReason { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### StreamingCompletionResponse
```csharp
public class StreamingCompletionResponse
{
    public string Content { get; set; }
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
    public int CumulativeTokens { get; set; }
}
```

### Provider
```csharp
public class Provider
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> SupportedModels { get; set; }
    public string Endpoint { get; set; }
    public bool SupportsStreaming { get; set; }
    public Dictionary<string, object> Capabilities { get; set; }
}
```

### ProviderConfiguration
```csharp
public class ProviderConfiguration
{
    public string ProviderId { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public string? Endpoint { get; set; }
    public int MaxRetries { get; set; }
    public TimeSpan Timeout { get; set; }
}
```

---

## Architecture

```
LLMGateway (Main Gateway)
├── ILLMProvider (Interface)
│   ├── OpenAIProvider
│   ├── AnthropicProvider
│   ├── GoogleProvider
│   └── OllamaProvider
├── Provider Registry
├── Request Router
└── Context Manager Integration
```

### Request Flow

```
ChatPane (Task 3.1)
    ↓ MessageSent event
    LLMGateway.SendMessage()
    ↓ GetContext() from IContextProvider
    CompletionRequest (with context)
    ↓ Route to active provider
    ILLMProvider.CompleteAsync()
    ↓ Provider-specific API call
    CompletionResponse
    ↓ MessageReceived event
    ChatPane.AddAssistantMessage()
```

### Streaming Flow

```
ChatPane.SendMessage()
    ↓
    LLMGateway.SendMessageAsync(stream: true)
    ↓
    ILLMProvider.StreamCompleteAsync()
    ↓ (chunks)
    ChatPane.UpdateLastAssistantMessage()
    ↓ (complete)
    ChatPane.CompleteLastAssistantMessage()
```

---

## Interface Definition

### ILLMProvider
```csharp
public interface ILLMProvider
{
    /// <summary>
    /// Gets provider information
    /// </summary>
    Provider GetProviderInfo();

    /// <summary>
    /// Configures the provider
    /// </summary>
    void Configure(ProviderConfiguration configuration);

    /// <summary>
    /// Sends a completion request
    /// </summary>
    Task<CompletionResponse> CompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a streaming completion request
    /// </summary>
    IAsyncEnumerable<StreamingCompletionResponse> StreamCompleteAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates token count for text
    /// </summary>
    int EstimateTokenCount(string text);

    /// <summary>
    /// Tests provider connection
    /// </summary>
    Task<bool> TestConnectionAsync(
        CancellationToken cancellationToken = default);
}
```

---

## Integration Points

### Dependencies
- Task 2.4: IContextProvider (ready to use)
- Task 3.1: ChatPane (event system ready)

### Events
- `MessageSent`: From ChatPane → LLMGateway
- `MessageReceived`: From LLMGateway → ChatPane
- `ErrorOccurred`: From LLMGateway → UI

### Services Used
- `IContextProvider`: For terminal context
- `ILogger`: For logging

---

## Provider Implementation Plan

### OpenAI Provider (Task 3.3)
- HTTP client for OpenAI API
- GPT-3.5, GPT-4 support
- Streaming support
- Error handling

### Future Providers
- **Anthropic**: Claude models
- **Google**: PaLM, Gemini
- **Ollama**: Local models (qwen2.5-coder, deepseek-r1)
- **Azure OpenAI**: Enterprise deployment
- **Hugging Face**: Open-source models

---

## Technical Considerations

### Token Counting
- Each provider may have different tokenization
- Default to approximation (~4 chars/token)
- Provider-specific counting when available
- Context truncation based on limits

### Retry Logic
- Exponential backoff
- Max retry attempts (configurable)
- Retryable vs non-retryable errors
- Circuit breaker pattern

### Streaming Implementation
- IAsyncEnumerable for async streams
- Chunk aggregation
- Buffer size management
- Error handling mid-stream

### Context Injection
- System prompt with context instructions
- Format: "Terminal context:\n{context}\n\nUser question:\n{question}"
- Truncate context if too long
- Respect max token limits

### Error Handling
- Provider-specific exceptions
- Timeout exceptions
- Rate limiting
- Authentication errors

---

## Acceptance Criteria

- [ ] ILLMProvider interface defined
- [ ] LLMGateway can register multiple providers
- [ ] LLMGateway can route requests to active provider
- [ ] LLMGateway can send completion requests
- [ ] LLMGateway can send streaming requests
- [ ] LLMGateway integrates with IContextProvider
- [ ] LLMGateway has configuration support
- [ ] Provider registration system works
- [ ] Request and response models defined
- [ ] Error handling implemented
- [ ] Retry logic implemented
- [ ] Interface is extensible for new providers

---

## Next Steps

After Task 3.2 is complete:
1. Task 3.3: Integrate OpenAI Provider
2. Task 3.4: Implement Message History and Syntax Highlighting

---

## Files to Create

```
/src/LLMGateway/ILLMProvider.cs
/src/LLMGateway/LLMGateway.cs
/src/LLMGateway/ProviderConfiguration.cs
/src/LLMGateway/CompletionRequest.cs
/src/LLMGateway/CompletionResponse.cs
/src/LLMGateway/StreamingCompletionResponse.cs
/src/LLMGateway/Models/Provider.cs
/src/LLMGateway/Models/Message.cs
/src/LLMGateway/LLMGatewayExceptions.cs
```

---

## Dependencies

### Internal
- Task 2.4: IContextProvider (ready)
- Task 3.1: ChatModels (Message model)

### External Libraries
- HttpClient or RestSharp (for API calls)
- System.Text.Json (for JSON serialization)

---

## Estimated Complexity
- **ILLMProvider.cs**: Low (~50 lines)
- **LLMGateway.cs**: High (~300 lines)
- **ProviderConfiguration.cs**: Low (~80 lines)
- **CompletionRequest.cs**: Low (~60 lines)
- **CompletionResponse.cs**: Low (~50 lines)
- **StreamingCompletionResponse.cs**: Low (~40 lines)
- **Models/Provider.cs**: Low (~60 lines)
- **Models/Message.cs**: Low (~50 lines)
- **LLMGatewayExceptions.cs**: Low (~80 lines)

**Total Estimated:** ~770 lines of C# code

---

## Known Issues

### Token Counting
- Different providers have different tokenization
- Need provider-specific implementations
- Approximation as fallback

### Context Size
- Different models have different context windows
- Need model-specific limits
- Dynamic truncation needed

### Rate Limiting
- Different providers have different rate limits
- Need per-provider rate limiting
- Need backpressure handling

---

## Testing Strategy

### Unit Tests
- Provider registration
- Request routing
- Context integration
- Token counting
- Error handling
- Retry logic

### Integration Tests
- OpenAI provider connectivity
- Streaming responses
- Context injection
- Token limit enforcement

---

## Mockup

```
LLMGateway Usage:

var gateway = new LLMGateway(logger, contextProvider);
var config = new ProviderConfiguration
{
    ProviderId = "openai",
    ApiKey = "sk-...",
    Model = "gpt-4",
    Temperature = 0.7,
    MaxTokens = 2000
};

gateway.Configure(config);

var request = new CompletionRequest
{
    Messages = new List<Message>
    {
        new Message { Role = "user", Content = "How do I list files?" }
    },
    Stream = true
};

await foreach (var chunk in gateway.SendAsync(request))
{
    chatPane.UpdateLastAssistantMessage(chunk.Content);
}

chatPane.CompleteLastAssistantMessage();
```

