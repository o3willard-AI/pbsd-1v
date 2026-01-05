namespace PairAdmin.LLMGateway.Models;

/// <summary>
/// LLM provider information
/// </summary>
public class Provider
{
    /// <summary>
    /// Unique provider identifier
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Provider display name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Provider description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// List of supported models
    /// </summary>
    public List<string> SupportedModels { get; set; }

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Whether provider supports streaming
    /// </summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Provider capabilities
    /// </summary>
    public Dictionary<string, object> Capabilities { get; set; }

    /// <summary>
    /// Maximum context window (in tokens)
    /// </summary>
    public int MaxContextTokens { get; set; }

    /// <summary>
    /// Default model
    /// </summary>
    public string DefaultModel { get; set; }

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    public Provider()
    {
        Id = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        SupportedModels = new List<string>();
        Endpoint = string.Empty;
        Capabilities = new Dictionary<string, object>();
        MaxContextTokens = 4096;
        DefaultModel = string.Empty;
    }

    /// <summary>
    /// Checks if a model is supported
    /// </summary>
    public bool SupportsModel(string model)
    {
        return SupportedModels.Contains(model, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the model-specific max context
    /// </summary>
    public int GetMaxContextForModel(string model)
    {
        if (Capabilities.TryGetValue($"max_context_{model}", out var value) && value is int intVal)
        {
            return intVal;
        }

        return MaxContextTokens;
    }

    /// <summary>
    /// Gets a capability value
    /// </summary>
    public T? GetCapability<T>(string key)
    {
        if (Capabilities.TryGetValue(key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }
}
