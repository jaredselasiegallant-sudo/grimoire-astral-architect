using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A persistent crafting recipe. Players discover recipes through
/// expeditions and narrative progression; each recipe defines
/// the inputs and output of an alchemical combination.
/// </summary>
public sealed class CraftingRecipe
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string Description { get; init; }

    /// <summary>First ingredient element (or Any for flexible recipes).</summary>
    public ElementType IngredientAElement { get; init; }

    /// <summary>Second ingredient element.</summary>
    public ElementType IngredientBElement { get; init; }

    /// <summary>Minimum rarity of first ingredient.</summary>
    public Rarity IngredientAMinRarity { get; init; } = Rarity.Common;

    /// <summary>Minimum rarity of second ingredient.</summary>
    public Rarity IngredientBMinRarity { get; init; } = Rarity.Common;

    /// <summary>Output item name.</summary>
    public required string OutputName { get; init; }

    /// <summary>Output element.</summary>
    public ElementType OutputElement { get; init; }

    /// <summary>Output mana power value.</summary>
    public int OutputManaPower { get; init; }

    /// <summary>Output rarity (may be higher than ingredients).</summary>
    public Rarity OutputRarity { get; init; }

    /// <summary>Quantity produced per craft.</summary>
    public int OutputQuantity { get; init; } = 1;

    /// <summary>Whether this recipe has been discovered by the player.</summary>
    public bool IsDiscovered { get; set; }

    /// <summary>Whether this recipe is currently unlocked for use.</summary>
    public bool IsUnlocked { get; set; }
}
