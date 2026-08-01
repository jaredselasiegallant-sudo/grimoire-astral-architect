using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A structure placed on the 2D sanctuary grid.
/// Each building provides passive bonuses, crafting capability, or familiar slots.
/// </summary>
public sealed class SanctuaryBuilding
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BuildingType Type { get; init; }
    public required string Name { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int Level { get; set; } = 1;
    public int MaxLevel { get; set; } = 10;

    /// <summary>Passive mana generation per second at current level.</summary>
    public double ManaPerSecond { get; set; }

    /// <summary>Number of familiar habitat slots (only meaningful for FamiliarHabitat type).</summary>
    public int HabitatSlots { get; set; }

    /// <summary>UTC timestamp when an upgrade will complete (null if not upgrading).</summary>
    public DateTimeOffset? UpgradeFinishUTC { get; set; }

    /// <summary>UTC timestamp when this building last produced resources.</summary>
    public DateTimeOffset? LastProductionUTC { get; set; }
}
