namespace PairAdmin.Context;

/// <summary>
/// Mode for context window sizing
/// </summary>
public enum ContextSizeMode
{
    Auto,       // Automatically determine based on model
    Fixed,      // Fixed number of tokens/lines
    Percentage   // Percentage of model's max context
}

/// <summary>
/// Reason for context truncation
/// </summary>
public enum TruncationReason
{
    None,         // No truncation needed
    TokenLimit,   // Truncated due to token limit
    LineLimit,    // Truncated due to line limit
    ModelLimit,    // Truncated due to model limit
    UserLimit      // Truncated due to user setting
}

/// <summary>
/// Policy for managing context window size
/// </summary>
public class ContextSizePolicy
{
    /// <summary>
    /// Size mode (Auto/Fixed/Percentage)
    /// </summary>
    public ContextSizeMode Mode { get; set; }

    /// <summary>
    /// Fixed size (in tokens or lines)
    /// </summary>
    public int FixedSize { get; set; }

    /// <summary>
    /// Percentage of model's max context (0.0 to 1.0)
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Minimum number of lines
    /// </summary>
    public int MinLines { get; set; }

    /// <summary>
    /// Maximum number of lines
    /// </summary>
    public int MaxLines { get; set; }

    /// <summary>
    /// Minimum number of tokens
    /// </summary>
    public int MinTokens { get; set; }

    /// <summary>
    /// Maximum number of tokens
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Model-specific default limits
    /// </summary>
    private static readonly Dictionary<string, int> ModelDefaults = new()
    {
        { "gpt-3.5-turbo", 4096 },
        { "gpt-4", 8192 },
        { "gpt-4-turbo", 8192 },
        { "gpt-4-turbo-preview", 8192 },
        { "claude-3-opus", 200000 },
        { "claude-3-sonnet", 200000 }
    };

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public ContextSizePolicy()
    {
        Mode = ContextSizeMode.Auto;
        FixedSize = 2000;
        Percentage = 0.5;
        MinLines = 10;
        MaxLines = 500;
        MinTokens = 100;
        MaxTokens = 128000;
    }

    /// <summary>
    /// Gets target size based on mode and model max context
    /// </summary>
    /// <param name="modelMaxContext">Model's maximum context window</param>
    /// <returns>Target size in tokens</returns>
    public int GetTargetSize(int modelMaxContext)
    {
        return Mode switch
        {
            ContextSizeMode.Auto => GetAutoSize(modelMaxContext),
            ContextSizeMode.Fixed => GetFixedSize(modelMaxContext),
            ContextSizeMode.Percentage => GetPercentageSize(modelMaxContext),
            _ => GetAutoSize(modelMaxContext)
        };
    }

    private int GetAutoSize(int modelMaxContext)
    {
        var defaultSize = modelMaxContext;

        if (ModelDefaults.TryGetValue(GetModelKey(), out var modelDefault))
        {
            defaultSize = modelDefault;
        }

        return Math.Min(defaultSize, modelMaxContext);
    }

    private int GetFixedSize(int modelMaxContext)
    {
        var size = FixedSize;

        if (MinTokens > 0)
        {
            size = Math.Max(size, MinTokens);
        }

        if (MaxTokens > 0)
        {
            size = Math.Min(size, MaxTokens);
        }

        return Math.Min(size, modelMaxContext);
    }

    private int GetPercentageSize(int modelMaxContext)
    {
        var percentage = Math.Clamp(Percentage, 0.0, 1.0);
        var size = (int)(modelMaxContext * percentage);

        if (MinTokens > 0)
        {
            size = Math.Max(size, MinTokens);
        }

        if (MaxTokens > 0)
        {
            size = Math.Min(size, MaxTokens);
        }

        return Math.Min(size, modelMaxContext);
    }

    private string GetModelKey()
    {
        return string.Empty;

    }

    /// <summary>
    /// Validates the policy
    /// </summary>
    /// <param name="errorMessage">Error message if invalid</param>
    /// <returns>True if policy is valid</returns>
    public bool Validate(out string? errorMessage)
    {
        errorMessage = null;

        if (Mode == ContextSizeMode.Fixed && FixedSize <= 0)
        {
            errorMessage = "FixedSize must be positive";
            return false;
        }

        if (Percentage < 0.0 || Percentage > 1.0)
        {
            errorMessage = "Percentage must be between 0.0 and 1.0";
            return false;
        }

        if (MinLines < 0)
        {
            errorMessage = "MinLines cannot be negative";
            return false;
        }

        if (MaxLines > 0 && MaxLines < MinLines)
        {
            errorMessage = "MaxLines must be >= MinLines";
            return false;
        }

        if (MinTokens < 0)
        {
            errorMessage = "MinTokens cannot be negative";
            return false;
        }

        if (MaxTokens > 0 && MaxTokens < MinTokens)
        {
            errorMessage = "MaxTokens must be >= MinTokens";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a copy of this policy
    /// </summary>
    public ContextSizePolicy Clone()
    {
        return new ContextSizePolicy
        {
            Mode = Mode,
            FixedSize = FixedSize,
            Percentage = Percentage,
            MinLines = MinLines,
            MaxLines = MaxLines,
            MinTokens = MinTokens,
            MaxTokens = MaxTokens
        };
    }
}
