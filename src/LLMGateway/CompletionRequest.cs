namespace PairAdmin.LLMGateway;

/// <summary>
/// Completion request for LLM API
/// </summary>
public class CompletionRequest
{
    /// <summary>
    /// List of messages in the conversation
    /// </summary>
    public List<Models.Message> Messages { get; set; }

    /// <summary>
    /// Model to use for completion
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Sampling temperature (0.0 to 2.0)
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Nucleus sampling parameter (0.0 to 1.0)
    /// </summary>
    public double TopP { get; set; }

    /// <summary>
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// Optional terminal context to include
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Cancellation token for async operation
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Number of completions to generate
    /// </summary>
    public int N { get; set; }

    /// <summary>
    /// Presence penalty (0.0 to 2.0)
    /// </summary>
    public double PresencePenalty { get; set; }

    /// <summary>
    /// Frequency penalty (0.0 to 2.0)
    /// </summary>
    public double FrequencyPenalty { get; set; }

    /// <summary>
    /// Stop sequences
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public CompletionRequest()
    {
        Messages = new List<Models.Message>();
        Model = string.Empty;
        Temperature = 0.7;
        MaxTokens = 1000;
        TopP = 1.0;
        Stream = false;
        CancellationToken = CancellationToken.None;
        N = 1;
        PresencePenalty = 0.0;
        FrequencyPenalty = 0.0;
    }

    /// <summary>
    /// Initializes a new instance with user message
    /// </summary>
    public CompletionRequest(string userMessage) : this()
    {
        Messages.Add(Models.Message.UserMessage(userMessage));
    }

    /// <summary>
    /// Adds a system message
    /// </summary>
    public CompletionRequest AddSystemMessage(string content)
    {
        Messages.Insert(0, Models.Message.SystemMessage(content));
        return this;
    }

    /// <summary>
    /// Adds a user message
    /// </summary>
    public CompletionRequest AddUserMessage(string content)
    {
        Messages.Add(Models.Message.UserMessage(content));
        return this;
    }

    /// <summary>
    /// Adds an assistant message
    /// </summary>
    public CompletionRequest AddAssistantMessage(string content)
    {
        Messages.Add(Models.Message.AssistantMessage(content));
        return this;
    }

    /// <summary>
    /// Adds context as a system message
    /// </summary>
    public CompletionRequest AddContext(string context)
    {
        if (!string.IsNullOrWhiteSpace(context))
        {
            var contextMessage = $"Terminal context:\n{context}\n\n";
            AddSystemMessage(contextMessage);
        }
        return this;
    }

    /// <summary>
    /// Validates the request
    /// </summary>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(Model))
        {
            errorMessage = "Model is required";
            return false;
        }

        if (Messages.Count == 0)
        {
            errorMessage = "At least one message is required";
            return false;
        }

        if (Temperature < 0.0 || Temperature > 2.0)
        {
            errorMessage = "Temperature must be between 0.0 and 2.0";
            return false;
        }

        if (TopP < 0.0 || TopP > 1.0)
        {
            errorMessage = "TopP must be between 0.0 and 1.0";
            return false;
        }

        if (MaxTokens <= 0)
        {
            errorMessage = "MaxTokens must be positive";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the estimated total token count
    /// </summary>
    public int GetEstimatedTokenCount()
    {
        var totalChars = Messages.Sum(m => m.Content.Length);
        if (!string.IsNullOrWhiteSpace(Context))
        {
            totalChars += Context.Length;
        }
        return totalChars / 4;
    }

    /// <summary>
    /// Gets a copy of the request
    /// </summary>
    public CompletionRequest Clone()
    {
        return new CompletionRequest
        {
            Messages = Messages.Select(m => m.Clone()).ToList(),
            Model = Model,
            Temperature = Temperature,
            MaxTokens = MaxTokens,
            TopP = TopP,
            Stream = Stream,
            Context = Context,
            CancellationToken = CancellationToken,
            N = N,
            PresencePenalty = PresencePenalty,
            FrequencyPenalty = FrequencyPenalty,
            Stop = Stop?.ToList()
        };
    }
}
