using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Memories;

/// <summary>
/// A Memory Echo — a wordless, visual flashback triggered when a ruin is restored.
/// Shows what that spot looked like before the fall. Collectible and replayable.
/// </summary>
public sealed class MemoryEcho
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The grid position this echo is tied to.</summary>
    public int GridX { get; init; }
    public int GridY { get; init; }

    /// <summary>Building type that was restored to trigger this echo.</summary>
    public BuildingType? RestoredBuilding { get; init; }

    /// <summary>Narrative description of what the echo shows.</summary>
    public required string VisualDescription { get; init; }

    /// <summary>Duration of the echo animation in seconds.</summary>
    public float DurationSeconds { get; init; } = 4f;

    /// <summary>Whether this echo has been witnessed by the player.</summary>
    public bool HasBeenWitnessed { get; set; }

    /// <summary>Whether this echo can be replayed from the Grimoire.</summary>
    public bool IsReplayable { get; init; } = true;

    /// <summary>UTC when this echo was first triggered.</summary>
    public DateTimeOffset? FirstWitnessedUTC { get; set; }

    /// <summary>Colour palette for the ghostly animation.</summary>
    public string GhostTintHex { get; init; } = "#A0C0FF";
}

/// <summary>
/// Pre-defined memory echoes tied to specific building placements.
/// Each echo tells a small story about what was lost.
/// </summary>
public static class MemoryEchoLibrary
{
    public static List<MemoryEcho> GetAllEchoes() =>
    [
        new()
        {
            GridX = 7, GridY = 5,
            RestoredBuilding = BuildingType.ManaShrine,
            VisualDescription = "A ring of architects stands around this very spot, hands raised, channeling starlight into the shrine. Their robes shimmer with the same light that still pulses here.",
            DurationSeconds = 5f,
            GhostTintHex = "#64C8FF"
        },
        new()
        {
            GridX = 3, GridY = 3,
            RestoredBuilding = BuildingType.FamiliarHabitat,
            VisualDescription = "Dozens of familiars curl together in nesting hollows, their combined glow painting the walls in shifting colour. One raises its head, as if sensing you watching from the future.",
            DurationSeconds = 4.5f,
            GhostTintHex = "#64FF8A"
        },
        new()
        {
            GridX = 12, GridY = 7,
            RestoredBuilding = BuildingType.AlchemicalCauldron,
            VisualDescription = "An architect stirs the cauldron with a staff of living wood. The liquid inside shifts through every colour — then settles on one that doesn't have a name yet.",
            DurationSeconds = 4f,
            GhostTintHex = "#C864FF"
        },
        new()
        {
            GridX = 5, GridY = 2,
            RestoredBuilding = BuildingType.GardenOfWhispers,
            VisualDescription = "Plants grow in impossible spirals, whispering to each other in a language of rustling leaves. An architect kneels among them, listening with closed eyes.",
            DurationSeconds = 4f,
            GhostTintHex = "#8AFF8A"
        },
        new()
        {
            GridX = 10, GridY = 4,
            RestoredBuilding = BuildingType.StarlightObelisk,
            VisualDescription = "The obelisk hums at a frequency that makes the air visible — you can see sound waves rippling outward, each one carrying a fragment of starlight to the sanctuary's edges.",
            DurationSeconds = 3.5f,
            GhostTintHex = "#FFE864"
        },
        new()
        {
            GridX = 8, GridY = 8,
            RestoredBuilding = BuildingType.VoidAnchor,
            VisualDescription = "An architect drives the anchor into the ground with a single, deliberate motion. The Void recoils. For a moment, the boundary between here and everywhere is perfectly clear.",
            DurationSeconds = 4f,
            GhostTintHex = "#6A3DFF"
        },
        new()
        {
            GridX = 14, GridY = 1,
            RestoredBuilding = BuildingType.PotionStation,
            VisualDescription = "Steam rises from a dozen potions in various states of completion. An architect tastes one, pauses, then adds a single tear to the mixture. It turns gold.",
            DurationSeconds = 3.5f,
            GhostTintHex = "#FFD080"
        }
    ];

    /// <summary>Get the echo for a specific grid position, if one exists.</summary>
    public static MemoryEcho? GetEchoForPosition(int x, int y) =>
        GetAllEchoes().FirstOrDefault(e => e.GridX == x && e.GridY == y);
}
