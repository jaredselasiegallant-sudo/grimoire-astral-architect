using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A stackable resource or crafted item stored in the player's inventory.
/// Used in alchemical crafting, spell fuel, and building upgrades.
/// </summary>
public sealed class InventoryItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ElementType Element { get; init; }
    public Rarity Rarity { get; init; } = Rarity.Common;
    public int Quantity { get; set; }

    /// <summary>Flat power value used when this item is consumed as spell fuel.</summary>
    public int ManaPower { get; set; }

    /// <summary>Optional: the building type this item can be used to construct/upgrade.</summary>
    public BuildingType? ValidForBuilding { get; set; }
}
