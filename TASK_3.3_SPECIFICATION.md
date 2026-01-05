# Task 3.3 Specification: Integrate OpenAI Provider

## Task: Integrate OpenAI Provider with API Client

**Phase:** Phase 3: AI Interface
**Status:** In Progress
**Date:** January 4, 2026

---

## Description

Implement the first LLM provider using OpenAI's API. This provider will implement the ILLMProvider interface and provide real AI completions to the chat interface.

---

## Deliverables

### 1. OpenAIProvider.cs
Main provider class implementing ILLMProvider:
- OpenAI API integration
- GPT-3.5, GPT-4 support
- Streaming completions
- Error handling and retry logic

### 2. OpenAIClient.cs
HTTP client for OpenAI API:
- API request/response handling
- JSON serialization/deserialization
- Streaming response parsing

### 3. OpenAIModels.cs
OpenAI-specific models:
- Request models (chat completion request)
- Response models (chat completion response)
- Stream chunk models

---

## Requirements

### Functional Requirements

1. **OpenAI API Integration**
   - Chat Completions API endpoint
   - Support for GPT-3.5 and GPT-4 models
   - API key authentication
   - Organization ID support

2. **Completion Support**
   - Non-streaming completions
   - Streaming completions
   - Async/await support
   - Cancellation token support

3. **Model Support**
   - gpt-3.5-turbo
   - gpt-4
   - gpt-4-turbo
   - gpt-4-turbo-preview
   - Model selection via configuration

4. **Error Handling**
   - Authentication errors (401)
   - Rate limit errors (429)
   - Invalid request errors (400)
   - Timeout handling
   - Retry with exponential backoff

5. **Token Counting**
   - Use OpenAI's token counts from response
   - Fallback to estimation
   - Context truncation

6. **Context Integration**
   - System message for terminal context
   - Context formatting for OpenAI
   - Token limit respect

### Non-Functional Requirements

1. **Performance**
   - Efficient JSON parsing
   - Minimal latency
   - Connection reuse

2. **Reliability**
   - Connection error recovery
   - Timeout handling
   - Graceful degradation

3. **Security**
   - Secure API key handling
   - No API key logging
   - HTTPS only

---

## OpenAI API Reference

### Endpoint
```
POST https://api.openai.com/v1/chat/completions
```

### Headers
```
Content-Type: application/json
Authorization: Bearer {api_key}
OpenAI-Organization: {organization_id} (optional)
```

### Request Format
```json
{
  "model": "gpt-3.5-turbo",
  "messages": [
    {
      "role": "system",
      "content": "You are a helpful assistant..."
    },
    {
      "role": "user",
      "content": "How do I list files in Linux?"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1000,
  "top_p": 1.0,
  "stream": false
}
```

### Response Format
```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1677652288,
  "model": "gpt-3.5-turbo",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "To list files in Linux, you can use..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 42,
    "completion_tokens": 300,
    "total_tokens": 342
  }
}
```

### Streaming Format
```
data: {"id": "...", "object": "chat.completion.chunk", "choices": [...]}
data: {"id": "...", "object": "chat.completion.chunk", "choices": [...]}
data: [DONE]
```

---

## Data Models

### OpenAICompletionRequest
```csharp
public class OpenAICompletionRequest
{
    public string Model { get; set; }
    public List<OpenAIMessage> Messages { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public bool Stream { get; set; }
    public int N { get; set; }
    public double PresencePenalty { get; set; }
    public double FrequencyPenalty { get; set; }
    public List<string>? Stop { get; set; }
}
```

### OpenAIMessage
```csharp
public class OpenAIMessage
{
    public string Role { get; set; }
    public string Content { get; set; }
}
```

### OpenAICompletionResponse
```csharp
public class OpenAICompletionResponse
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<OpenAIChoice> Choices { get; set; }
    public OpenAIUsage Usage { get; set; }
}
```

### OpenAIChoice
```csharp
public class OpenAIChoice
{
    public int Index { get; set; }
    public OpenAIMessage Message { get; set; }
    public string FinishReason { get; set; }
}
```

### OpenAIUsage
```csharp
public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
```

### OpenAIStreamChunk
```csharp
public class OpenAIStreamChunk
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Model { get; set; }
    public List<OpenAIChoice> Choices { get; set; }
}
```

---

## Architecture

```
OpenAIProvider (Implements ILLMProvider)
├── OpenAIClient (HTTP Client)
│   ├── Request/Response Handling
│   ├── JSON Serialization
│   └── Stream Parsing
└── Configuration (ProviderConfiguration)
    ├── API Key
    ├── Model Selection
    └── Parameters
```

### Request Flow

```
LLMGateway.SendAsync()
    ↓
    OpenAIProvider.CompleteAsync()
    ↓ Convert to OpenAI request format
    OpenAIClient.SendRequestAsync()
    ↓ HTTP POST to OpenAI
    Parse JSON response
    ↓ Convert to LLMGateway format
    CompletionResponse
```

### Streaming Flow

```
LLMGateway.SendStreamingAsync()
    ↓
    OpenAIProvider.StreamCompleteAsync()
    ↓ Convert to OpenAI request format
    OpenAIClient.StreamRequestAsync()
    ↓ HTTP POST with stream: true
    Parse SSE (Server-Sent Events)
    ↓ (chunks)
    StreamingCompletionResponse
```

---

## Error Handling

### HTTP Status Codes
- **200**: Success
- **400**: Invalid request (non-retryable)
- **401**: Authentication failed (non-retryable)
- **429**: Rate limit exceeded (retryable)
- **500**: Server error (retryable)
- **503**: Service unavailable (retryable)

### Error Response Format
```json
{
  "error": {
    "message": "...",
    "type": "...",
    "param": "...",
    "code": "..."
  }
}
```

### Exception Mapping
- `400` → `LLMInvalidRequestException`
- `401` → `LLMAuthenticationException`
- `429` → `LLMRateLimitException`
- `Timeout` → `LLMTimeoutException`
- `Other` → `LLMProviderException`

---

## Integration Points

### Dependencies
- Task 3.2: ILLMProvider interface ✅
- Task 3.2: LLMGateway ✅
- System.Net.Http (HttpClient)
- System.Text.Json (JSON serialization)

### Integration with LLMGateway
```csharp
var gateway = new LLMGateway(logger, contextProvider);
var openaiProvider = new OpenAIProvider(logger);
gateway.RegisterProvider(openaiProvider);

var config = new ProviderConfiguration
{
    ProviderId = "openai",
    ApiKey = "sk-...",
    Model = "gpt-3.5-turbo",
    Temperature = 0.7,
    MaxTokens = 2000
};

gateway.ConfigureProvider(config);
gateway.SetActiveProvider("openai");
```

---

## Technical Considerations

### Token Counting
- Use OpenAI's `usage` field from response
- Prompt tokens from `usage.prompt_tokens`
- Completion tokens from `usage.completion_tokens`
- Fallback to estimation if not available

### Context Injection
- Add system message with context
- Format: "Terminal context:\n{context}\n\n"
- Respect OpenAI's token limits

### Streaming Implementation
- Use Server-Sent Events (SSE)
- Parse "data: " lines
- Handle "[DONE]" marker
- Aggregate delta content

### Rate Limiting
- Respect 429 responses
- Implement backoff
- Consider `Retry-After` header

### Model Selection
- Default: gpt-3.5-turbo
- Support: gpt-4, gpt-4-turbo, gpt-4-turbo-preview
- Model-specific context windows

---

## Acceptance Criteria

- [ ] OpenAIProvider implements ILLMProvider
- [ ] OpenAIProvider connects to OpenAI API
- [ ] Non-streaming completions work correctly
- [ ] Streaming completions work correctly
- [ ] Token counts from response are used
- [ ] Errors are handled gracefully
- [ ] Authentication errors are handled
- [ ] Rate limit errors are handled
- [ ] Timeout errors are handled
- [ ] Retry logic is implemented
- [ ] Configuration is validated
- [ ] Provider metadata is correct
- [ ] Context is included in requests
- [ ] Multiple models are supported

---

## Next Steps

After Task 3.3 is complete:
1. Task 3.4: Implement Message History and Syntax Highlighting

---

## Files to Create

```
/src/LLMGateway/Providers/OpenAIProvider.cs
/src/LLMGateway/Providers/OpenAIClient.cs
/src/LLMGateway/Providers/OpenAIModels.cs
```

---

## Dependencies

### Internal
- Task 3.2: ILLMProvider (interface)
- Task 3.2: ProviderConfiguration
- Task 3.2: CompletionRequest
- Task 3.2: CompletionResponse
- Task 3.2: StreamingCompletionResponse

### External Libraries
- System.Net.Http (HttpClient)
- System.Text.Json (JSON serialization)
- System.Text.RegularExpressions (SSE parsing)

---

## Estimated Complexity
- **OpenAIModels.cs**: Low (~120 lines)
- **OpenAIClient.cs**: Medium (~250 lines)
- **OpenAIProvider.cs**: High (~300 lines)

**Total Estimated:** ~670 lines of C# code

---

## Testing Strategy

### Unit Tests
- Request model serialization
- Response model deserialization
- SSE parsing
- Token counting
- Error mapping

### Integration Tests
- Mock OpenAI API
- Test with real API key (optional)
- Streaming response handling
- Error scenarios

---

## Known Issues

### TODO Items
1. **Model List:**
   - Fetch available models from OpenAI
   - Dynamic model discovery

2. **Fine-tuning:**
   - Support for fine-tuned models
   - Custom model handling

3. **Function Calling:**
   - Support for OpenAI function calling
   - Tool use integration

4. **Assistants API:**
   - Support for OpenAI Assistants
   - Thread management

---

## Mockup

```
OpenAIProvider Usage:

var provider = new OpenAIProvider(logger);
var config = new ProviderConfiguration
{
    ProviderId = "openai",
    ApiKey = "sk-abc123",
    Model = "gpt-3.5-turbo",
    Temperature = 0.7,
    MaxTokens = 2000
};

provider.Configure(config);

var request = new CompletionRequest
{
    Messages = new List<Message>
    {
        Message.UserMessage("How do I list files in Linux?")
    }
};

var response = await provider.CompleteAsync(request);

Console.WriteLine(response.Content);
Console.WriteLine($"Tokens: {response.TotalTokens}");
Console.WriteLine($"Cost: ${response.GetEstimatedCost()}");
```

