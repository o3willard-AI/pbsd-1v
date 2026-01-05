namespace PairAdmin.Help;

/// <summary>
/// Service for managing tutorials
/// </summary>
public class TutorialService
{
    private readonly HelpService _helpService;
    private readonly Dictionary<string, TutorialProgress> _progress;
    private readonly ILogger<TutorialService>? _logger;

    public TutorialService(HelpService helpService, ILogger<TutorialService>? logger = null)
    {
        _helpService = helpService;
        _logger = logger;
        _progress = new Dictionary<string, TutorialProgress>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a tutorial by ID
    /// </summary>
    public Tutorial? GetTutorial(string tutorialId)
    {
        return _helpService.GetTutorial(tutorialId);
    }

    /// <summary>
    /// Gets all available tutorials
    /// </summary>
    public IEnumerable<Tutorial> GetAvailableTutorials()
    {
        return _helpService.GetTutorials();
    }

    /// <summary>
    /// Starts a tutorial
    /// </summary>
    public TutorialProgress? StartTutorial(string tutorialId)
    {
        var tutorial = _helpService.GetTutorial(tutorialId);
        if (tutorial == null)
        {
            _logger?.LogWarning("Tutorial not found: {TutorialId}", tutorialId);
            return null;
        }

        var progress = new TutorialProgress
        {
            TutorialId = tutorialId,
            TutorialName = tutorial.Title,
            StartedAt = DateTime.UtcNow,
            CurrentStep = 0,
            TotalSteps = tutorial.TotalSteps,
            IsCompleted = false
        };

        _progress[tutorialId] = progress;
        _logger?.LogInformation("Started tutorial: {TutorialName}", tutorial.Title);

        return progress;
    }

    /// <summary>
    /// Gets progress for a tutorial
    /// </summary>
    public TutorialProgress? GetProgress(string tutorialId)
    {
        return _progress.TryGetValue(tutorialId, out var progress) ? progress : null;
    }

    /// <summary>
    /// Gets all tutorial progress
    /// </summary>
    public IEnumerable<TutorialProgress> GetAllProgress()
    {
        return _progress.Values;
    }

    /// <summary>
    /// Advances to the next step
    /// </summary>
    public TutorialProgress? AdvanceStep(string tutorialId)
    {
        if (!_progress.TryGetValue(tutorialId, out var progress))
            return null;

        progress.CurrentStep++;
        
        if (progress.CurrentStep >= progress.TotalSteps)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            _logger?.LogInformation("Completed tutorial: {TutorialName}", progress.TutorialName);
        }

        return progress;
    }

    /// <summary>
    /// Resets tutorial progress
    /// </summary>
    public bool ResetProgress(string tutorialId)
    {
        if (_progress.Remove(tutorialId))
        {
            _logger?.LogInformation("Reset progress for tutorial: {TutorialId}", tutorialId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets completed tutorials
    /// </summary>
    public IEnumerable<TutorialProgress> GetCompletedTutorials()
    {
        return _progress.Values.Where(p => p.IsCompleted);
    }

    /// <summary>
    /// Gets in-progress tutorials
    /// </summary>
    public IEnumerable<TutorialProgress> GetInProgressTutorials()
    {
        return _progress.Values.Where(p => !p.IsCompleted);
    }
}

/// <summary>
/// Tracks tutorial progress
/// </summary>
public class TutorialProgress
{
    /// <summary>
    /// Tutorial ID
    /// </summary>
    public string TutorialId { get; set; } = string.Empty;

    /// <summary>
    /// Tutorial name
    /// </summary>
    public string TutorialName { get; set; } = string.Empty;

    /// <summary>
    /// When the tutorial was started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the tutorial was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current step number (0-based)
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Whether the tutorial is completed
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Progress percentage
    /// </summary>
    public int ProgressPercentage => TotalSteps > 0 
        ? (int)((double)CurrentStep / TotalSteps * 100) 
        : 0;

    /// <summary>
    /// Gets a status string
    /// </summary>
    public string GetStatusString()
    {
        if (IsCompleted)
            return "Completed";
        if (CurrentStep == 0)
            return "Not Started";
        return $"Step {CurrentStep + 1}/{TotalSteps}";
    }
}
