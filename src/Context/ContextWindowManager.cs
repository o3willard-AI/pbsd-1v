using Microsoft.Extensions.Logging;

namespace PairAdmin.Context;

/// <summary>
/// Settings for context window
/// </summary>
public class ContextWindowSettings
{
    /// <summary>
    /// Size mode (Auto/Fixed/Percentage)
    /// </summary>
    public ContextSizeMode SizeMode { get; set; }

    /// <summary>
    /// Fixed size (in tokens)
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
    /// Include system prompt in context
    /// </summary>
    public bool IncludeSystemPrompt { get; set; }

    /// <summary>
    /// Include working directory in context
    /// </summary>
    public bool IncludeWorkingDirectory { get; set; }

    /// <summary>
    /// Include privilege level in context
    /// </summary>
    public bool IncludePrivilegeLevel { get; set; }

    /// <summary>
    /// Initializes a new instance with default values
    /// </summary>
    public ContextWindowSettings()
    {
        SizeMode = ContextSizeMode.Auto;
        FixedSize = 2000;
        Percentage = 0.5;
        MinLines = 10;
        MaxLines = 500;
        MinTokens = 100;
        MaxTokens = 128000;
        IncludeSystemPrompt = true;
        IncludeWorkingDirectory = true;
        IncludePrivilegeLevel = true;
    }

    /// <summary>
    /// Creates a copy of this settings
    /// </summary>
    public ContextWindowSettings Clone()
    {
        return new ContextWindowSettings
        {
            SizeMode = SizeMode,
            FixedSize = FixedSize,
            Percentage = Percentage,
            MinLines = MinLines,
            MaxLines = MaxLines,
            MinTokens = MinTokens,
            MaxTokens = MaxTokens,
            IncludeSystemPrompt = IncludeSystemPrompt,
            IncludeWorkingDirectory = IncludeWorkingDirectory,
            IncludePrivilegeLevel = IncludePrivilegeLevel
        };
    }
}

/// <summary>
/// Main manager for context window management
/// </summary>
public class ContextWindowManager
{
    private readonly IContextProvider _contextProvider;
    private readonly ContextSizePolicy _policy;
    private readonly ILogger<ContextWindowManager> _logger;
    private int _modelMaxContext;

    /// <summary>
    /// Event raised when context is truncated
    /// </summary>
    public event EventHandler<TruncationResult>? ContextTruncated;

    /// <summary>
    /// Gets the current policy
    /// </summary>
    public ContextSizePolicy Policy => _policy;

    /// <summary>
    /// Gets the maximum number of lines
    /// </summary>
    public int MaxLines => _policy.MaxLines;

    /// <summary>
    /// Gets the current number of lines in context
    /// </summary>
    public int CurrentLines => _contextProvider.GetContext().Split('\n').Length;

    /// <summary>
    /// Gets the current token count
    /// </summary>
    public int CurrentTokens => GetContextSize();

    /// <summary>
    /// Initializes a new instance of ContextWindowManager
    /// </summary>
    public ContextWindowManager(
        IContextProvider contextProvider,
        ContextSizePolicy policy,
        ILogger<ContextWindowManager> logger)
    {
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelMaxContext = 4096;

        _logger.LogInformation($"ContextWindowManager initialized with mode: {_policy.Mode}");
    }

    /// <summary>
    /// Sets the model's maximum context window
    /// </summary>
    /// <param name="maxContext">Model's maximum context in tokens</param>
    public void SetModelMaxContext(int maxContext)
    {
        _modelMaxContext = maxContext;
        _logger.LogInformation($"Model max context set to {maxContext}");
    }

    /// <summary>
    /// Gets truncated context based on policy
    /// </summary>
    /// <param name="maxTokens">Maximum tokens allowed</param>
    /// <returns>TruncationResult with truncated context</returns>
    public TruncationResult GetTruncatedContext(int maxTokens)
    {
        var targetSize = _policy.GetTargetSize(_modelMaxContext);
        var effectiveMax = Math.Min(targetSize, maxTokens);

        var context = _contextProvider.GetContext();
        var estimatedTokens = EstimateTokenCount(context);

        if (estimatedTokens <= effectiveMax)
        {
            return new TruncationResult
            {
                TruncatedContext = context,
                OriginalTokens = estimatedTokens,
                TruncatedTokens = estimatedTokens,
                WasTruncated = false,
                Reason = TruncationReason.None
            };
        }

        return TruncateContext(context, estimatedTokens, effectiveMax);
    }

    /// <summary>
    /// Gets truncated context with target size
    /// </summary>
    /// <param name="targetSize">Target size in tokens</param>
    /// <returns>TruncationResult with truncated context</returns>
    public TruncationResult GetTruncatedContextWithSize(int targetSize)
    {
        var context = _contextProvider.GetContext();
        var estimatedTokens = EstimateTokenCount(context);

        if (estimatedTokens <= targetSize)
        {
            return new TruncationResult
            {
                TruncatedContext = context,
                OriginalTokens = estimatedTokens,
                TruncatedTokens = estimatedTokens,
                WasTruncated = false,
                Reason = TruncationReason.None
            };
        }

        return TruncateContext(context, estimatedTokens, targetSize);
    }

    /// <summary>
    /// Gets the current context size estimate
    /// </summary>
    /// <returns>Estimated token count</returns>
    public int GetContextSize()
    {
        var context = _contextProvider.GetContext();
        return EstimateTokenCount(context);
    }

    /// <summary>
    /// Gets the target context size
    /// </summary>
    /// <returns>Target size in tokens</returns>
    public int GetTargetSize()
    {
        return _policy.GetTargetSize(_modelMaxContext);
    }

    /// <summary>
    /// Gets the effective maximum context size
    /// </summary>
    /// <returns>Effective maximum in tokens</returns>
    public int GetEffectiveMax()
    {
        var targetSize = _policy.GetTargetSize(_modelMaxContext);

        if (_policy.MaxTokens > 0)
        {
            return Math.Min(targetSize, _policy.MaxTokens);
        }

        return targetSize;
    }

    private TruncationResult TruncateContext(string context, int currentTokens, int targetTokens)
    {
        var lines = context.Split('\n');
        var tokenPerLine = currentTokens > 0 ? (double)currentTokens / lines.Length : 0;
        var targetLines = (int)(targetTokens / Math.Max(tokenPerLine, 1));

        if (targetLines >= lines.Length)
        {
            return new TruncationResult
            {
                TruncatedContext = context,
                OriginalTokens = currentTokens,
                TruncatedTokens = currentTokens,
                WasTruncated = false,
                Reason = TruncationReason.None
            };
        }

        var truncatedLines = lines.Skip(lines.Length - targetLines).Take(targetLines).ToList();
        var truncatedContext = string.Join('\n', truncatedLines);

        var result = new TruncationResult
        {
            TruncatedContext = truncatedContext,
            OriginalTokens = currentTokens,
            TruncatedTokens = EstimateTokenCount(truncatedContext),
            WasTruncated = true,
            Reason = DetermineTruncationReason(currentTokens, targetTokens)
        };

        _logger.LogWarning($"Context truncated: {result.RemovedTokens} tokens removed, " +
                          $"Reason: {result.Reason}");

        ContextTruncated?.Invoke(this, result);

        return result;
    }

    private TruncationReason DetermineTruncationReason(int currentTokens, int targetTokens)
    {
        if (targetTokens >= _modelMaxContext)
        {
            return TruncationReason.ModelLimit;
        }

        if (targetTokens >= _policy.MaxTokens && _policy.MaxTokens > 0)
        {
            return TruncationReason.UserLimit;
        }

        return TruncationReason.TokenLimit;
    }

    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Length / 4;
    }

    /// <summary>
    /// Updates the context size policy
    /// </summary>
    /// <param name="settings">New settings to apply</param>
    public void UpdatePolicy(ContextWindowSettings settings)
    {
        var newPolicy = new ContextSizePolicy
        {
            Mode = settings.SizeMode,
            FixedSize = settings.FixedSize,
            Percentage = settings.Percentage,
            MinLines = settings.MinLines,
            MaxLines = settings.MaxLines,
            MinTokens = settings.MinTokens,
            MaxTokens = settings.MaxTokens
        };

        if (!newPolicy.Validate(out var errorMessage))
        {
            _logger.LogError($"Invalid policy: {errorMessage}");
            throw new ArgumentException($"Invalid policy: {errorMessage}", nameof(settings));
        }

        _policy.Mode = newPolicy.Mode;
        _policy.FixedSize = newPolicy.FixedSize;
        _policy.Percentage = newPolicy.Percentage;
        _policy.MinLines = newPolicy.MinLines;
        _policy.MaxLines = newPolicy.MaxLines;
        _policy.MinTokens = newPolicy.MinTokens;
        _policy.MaxTokens = newPolicy.MaxTokens;

        _logger.LogInformation($"Policy updated: {_policy.Mode}");
    }

    /// <summary>
    /// Gets the current policy
    /// </summary>
    /// <returns>Current context size policy</returns>
    public ContextSizePolicy GetPolicy()
    {
        return _policy.Clone();
    }

    /// <summary>
    /// Checks if context will be truncated
    /// </summary>
    /// <param name="maxTokens">Maximum tokens allowed</param>
    /// <returns>True if context will be truncated</returns>
    public bool WillTruncate(int maxTokens)
    {
        var currentSize = GetContextSize();
        var effectiveMax = GetEffectiveMax();
        return currentSize > effectiveMax;
    }

    /// <summary>
    /// Clears the context
    /// </summary>
    public void Clear()
    {
        _contextProvider.Clear();
        _logger.LogInformation("Context cleared");
    }

    /// <summary>
    /// Sets the context size as a percentage of model max
    /// </summary>
    /// <param name="percentage">Percentage (0.0 to 1.0)</param>
    public void SetPercentage(double percentage)
    {
        _policy.Percentage = Math.Clamp(percentage, 0.0, 1.0);
        _policy.Mode = ContextSizeMode.Percentage;
        _logger.LogInformation($"Context size set to {percentage:P0} of model max");
    }
}
