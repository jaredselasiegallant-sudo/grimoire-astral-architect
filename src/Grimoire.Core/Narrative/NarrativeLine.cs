using Grimoire.Core.Enums;

namespace Grimoire.Core.Narrative;

/// <summary>
/// Represents a single line or beat in the narrative script.
/// Each line is triggered by a gameplay event or tutorial step.
/// </summary>
public sealed class NarrativeLine
{
    public required string Id { get; init; }
    public required string Speaker { get; init; }
    public required string Text { get; init; }
    public NarrativeType Type { get; init; } = NarrativeType.Narrator;
    public string? TriggerEvent { get; init; }
    public float DisplayDurationSeconds { get; init; } = 4f;
    public bool HasBeenShown { get; set; }
}

/// <summary>Who is speaking this narrative line.</summary>
public enum NarrativeType
{
    Narrator,
    Familiar,
    DistantVoice,
    System
}

/// <summary>
/// A chapter groups narrative lines that belong together thematically.
/// Chapters are unlocked by completing tutorial milestones.
/// </summary>
public sealed class NarrativeChapter
{
    public int Number { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public List<NarrativeLine> Lines { get; init; } = [];
    public bool IsUnlocked { get; set; }
    public bool IsComplete { get; set; }
}
