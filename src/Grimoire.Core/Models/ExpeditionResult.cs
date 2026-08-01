using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// Represents loot gathered during an idle expedition.
/// </summary>
public sealed class ExpeditionResult
{
    public Guid FamiliarId { get; init; }
    public DateTimeOffset DepartedUTC { get; init; }
    public DateTimeOffset ReturnedUTC { get; init; }
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }

    /// <summary>Items discovered during the expedition.</summary>
    public List<LootDrop> Loot { get; init; } = [];

    /// <summary>Experience earned by the familiar.</summary>
    public int ExperienceEarned { get; set; }

    /// <summary>Mana crystals gathered.</summary>
    public int ManaCrystalsEarned { get; set; }
}

/// <summary>A single item drop from an expedition.</summary>
public sealed class LootDrop
{
    public required string ItemName { get; init; }
    public ElementType Element { get; init; }
    public Rarity Rarity { get; init; }
    public int Quantity { get; init; }
}
