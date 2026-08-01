namespace Grimoire.Core.Models;

/// <summary>
/// A log entry from a completed expedition.
/// Displayed to the player on return and stored for review.
/// </summary>
public sealed class ExpeditionLogEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FamiliarName { get; init; }
    public DateTimeOffset DepartedUTC { get; init; }
    public DateTimeOffset ReturnedUTC { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }
    public int ManaCrystalsEarned { get; init; }
    public int ExperienceEarned { get; init; }
    public List<string> ItemNames { get; init; } = [];
    public string? NarrativeNote { get; init; }
}

/// <summary>
/// Game settings persisted to local storage (not SQLite).
/// Controls rendering quality, audio, and UX preferences.
/// </summary>
public sealed class GameSettings
{
    public int TargetFps { get; set; } = 60;
    public int ParticleDensity { get; set; } = 100; // percentage
    public float MusicVolume { get; set; } = 0.7f;
    public float SfxVolume { get; set; } = 0.8f;
    public bool ShowTutorialHints { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool EnableGestureTrail { get; set; } = true;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; } = 1400;
    public int WindowHeight { get; set; } = 800;
    public bool IsMaximized { get; set; }
}
