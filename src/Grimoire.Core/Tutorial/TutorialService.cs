namespace Grimoire.Core.Tutorial;

/// <summary>
/// Represents a single tutorial step that must be completed
/// before the player advances to the next phase.
/// </summary>
public sealed class TutorialStep
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Instruction { get; init; }
    public required string TargetAction { get; init; }
    public string? HighlightElement { get; init; }
    public bool IsCompleted { get; set; }
    public int Order { get; init; }
}

/// <summary>
/// Tracks tutorial progress and determines which step is currently active.
/// The tutorial is non-blocking — players can ignore it, but steps
/// unlock new functionality when completed.
/// </summary>
public sealed class TutorialService
{
    private readonly List<TutorialStep> _steps;
    private int _currentStepIndex;

    /// <summary>Fired when a new tutorial step becomes active.</summary>
    public event Action<TutorialStep>? OnStepActivated;

    /// <summary>Fired when the tutorial is fully complete.</summary>
    public event Action? OnTutorialComplete;

    public TutorialStep? CurrentStep => _currentStepIndex < _steps.Count ? _steps[_currentStepIndex] : null;
    public bool IsComplete => _currentStepIndex >= _steps.Count;
    public float Progress => _steps.Count == 0 ? 1f : (float)_currentStepIndex / _steps.Count;

    public TutorialService()
    {
        _steps = CreateDefaultSteps();
        _currentStepIndex = 0;
    }

    /// <summary>
    /// Call this when the player performs an action.
    /// If it matches the current step's target, the step is completed
    /// and the next step activates.
    /// </summary>
    public bool TryCompleteStep(string actionName)
    {
        if (IsComplete) return false;

        var step = _steps[_currentStepIndex];
        if (step.TargetAction != actionName) return false;

        step.IsCompleted = true;
        _currentStepIndex++;

        if (IsComplete)
        {
            OnTutorialComplete?.Invoke();
        }
        else
        {
            OnStepActivated?.Invoke(_steps[_currentStepIndex]);
        }

        return true;
    }

    /// <summary>Skip the entire tutorial (for returning players).</summary>
    public void SkipTutorial()
    {
        foreach (var step in _steps)
            step.IsCompleted = true;
        _currentStepIndex = _steps.Count;
        OnTutorialComplete?.Invoke();
    }

    /// <summary>Serialise completed step IDs for save persistence.</summary>
    public List<string> GetCompletedStepIds() =>
        _steps.Where(s => s.IsCompleted).Select(s => s.Id).ToList();

    /// <summary>Restore tutorial progress from a save.</summary>
    public void RestoreProgress(IEnumerable<string> completedStepIds)
    {
        var completed = new HashSet<string>(completedStepIds);
        foreach (var step in _steps)
        {
            if (completed.Contains(step.Id))
                step.IsCompleted = true;
        }
        _currentStepIndex = _steps.FindIndex(s => !s.IsCompleted);
        if (_currentStepIndex < 0) _currentStepIndex = _steps.Count;
    }

    private static List<TutorialStep> CreateDefaultSteps() =>
    [
        new TutorialStep
        {
            Id = "tap_shrine",
            Title = "Awaken the Shrine",
            Instruction = "Tap the glowing shrine at the center of your sanctuary.",
            TargetAction = "shrine_tapped",
            HighlightElement = "center_shrine",
            Order = 0
        },
        new TutorialStep
        {
            Id = "collect_mana",
            Title = "Gather Mana Shards",
            Instruction = "Collect the Mana Shards scattered by the shrine.",
            TargetAction = "mana_collected",
            HighlightElement = "mana_shards",
            Order = 1
        },
        new TutorialStep
        {
            Id = "feed_egg",
            Title = "Nurture the Egg",
            Instruction = "Feed Mana Shards to the cracked egg in the Familiar Bar.",
            TargetAction = "egg_fed",
            HighlightElement = "familiar_egg",
            Order = 2
        },
        new TutorialStep
        {
            Id = "name_familiar",
            Title = "Name Your Familiar",
            Instruction = "Give your new companion a name.",
            TargetAction = "familiar_named",
            HighlightElement = "name_input",
            Order = 3
        },
        new TutorialStep
        {
            Id = "draw_circle",
            Title = "Trace the Circle",
            Instruction = "Draw a circle gesture on the sky to cast a Ward.",
            TargetAction = "circle_gesture",
            HighlightElement = "game_canvas",
            Order = 4
        },
        new TutorialStep
        {
            Id = "open_cauldron",
            Title = "Discover the Cauldron",
            Instruction = "Open the Alchemical Cauldron panel.",
            TargetAction = "cauldron_opened",
            HighlightElement = "cauldron_panel",
            Order = 5
        },
        new TutorialStep
        {
            Id = "craft_potion",
            Title = "Brew a Potion",
            Instruction = "Combine two items in the Cauldron to craft a potion.",
            TargetAction = "potion_crafted",
            HighlightElement = "cauldron_panel",
            Order = 6
        },
        new TutorialStep
        {
            Id = "send_expedition",
            Title = "First Expedition",
            Instruction = "Send your familiar on an idle expedition.",
            TargetAction = "expedition_sent",
            HighlightElement = "familiar_bar",
            Order = 7
        }
    ];
}
