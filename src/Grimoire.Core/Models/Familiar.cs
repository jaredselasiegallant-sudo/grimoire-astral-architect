using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A digital companion the player hatches, nurtures, and sends on idle expeditions.
/// </summary>
public sealed class Familiar
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public FamiliarType Type { get; init; }
    public ElementType Element { get; init; }
    public Rarity Rarity { get; init; } = Rarity.Common;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int MaxHealth { get; set; } = 100;
    public int CurrentHealth { get; set; } = 100;
    public double GatheringBonus { get; set; } = 1.0;

    public DateTimeOffset? LastExpeditionUTC { get; set; }
    public TimeSpan ExpeditionDuration { get; set; }
    public bool IsOnExpedition { get; set; }
    public DateTimeOffset? ExpeditionReturnUTC { get; set; }

    /// <summary>Expedition types this familiar excels at (gives bonus rewards).</summary>
    public List<ExpeditionType> Specializations { get; set; } = [];

    /// <summary>Favorite item type (gives bonus happiness when fed).</summary>
    public ElementType? FavoriteElement { get; set; }

    /// <summary>Custom color tint applied to this familiar's rendering.</summary>
    public string? CustomColourHex { get; set; }

    /// <summary>Equipped cosmetic accessory ID.</summary>
    public Guid? EquippedAccessoryId { get; set; }

    /// <summary>
    /// Get the specialization bonus for a given expedition type.
    /// </summary>
    public double GetExpeditionBonus(ExpeditionType type)
    {
        if (Specializations.Contains(type))
        {
            return 1.0 + (0.2 * Level) + (Rarity switch
            {
                Rarity.Uncommon => 0.1,
                Rarity.Rare => 0.2,
                Rarity.Epic => 0.3,
                Rarity.Legendary => 0.5,
                _ => 0.0
            });
        }
        return 1.0;
    }

    /// <summary>
    /// Assign default specializations based on familiar type.
    /// </summary>
    public void InitialiseSpecializations()
    {
        Specializations = Type switch
        {
            FamiliarType.Wisp => [ExpeditionType.Gathering, ExpeditionType.Arcane],
            FamiliarType.Sprite => [ExpeditionType.Gathering, ExpeditionType.Soothing],
            FamiliarType.Drakling => [ExpeditionType.Exploration, ExpeditionType.Delving],
            FamiliarType.Mothwing => [ExpeditionType.Soothing, ExpeditionType.Exploration],
            FamiliarType.Golem => [ExpeditionType.Delving, ExpeditionType.Gathering],
            FamiliarType.Shade => [ExpeditionType.Delving, ExpeditionType.Arcane],
            FamiliarType.Foxfire => [ExpeditionType.Exploration, ExpeditionType.Arcane],
            _ => []
        };
    }
}
