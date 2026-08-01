using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Services;

/// <summary>
/// Deterministic calculator for idle expedition rewards.
/// Uses diminishing returns curve so long absences feel fair, not exploitable.
/// </summary>
public static class IdleRewardCalculator
{
    private const double XpPerSecond = 0.5;
    private const double ManaPerSecond = 0.2;
    private const double LootChancePerWindow = 0.15;
    private const int LootWindowSeconds = 10;
    private static readonly TimeSpan MaxExpeditionDuration = TimeSpan.FromHours(24);

    /// <summary>
    /// Diminishing returns rate: full rate for first 2h, 70% for 2-8h, 40% for 8-24h.
    /// Prevents exploitation by long absences while keeping it fair.
    /// </summary>
    public static double GetDiminishingReturnsRate(TimeSpan elapsed)
    {
        var hours = elapsed.TotalHours;
        if (hours <= 2) return 1.0;
        if (hours <= 8) return 0.7;
        return 0.4;
    }

    public static ExpeditionResult Calculate(Familiar familiar, DateTimeOffset nowUTC)
    {
        if (familiar.LastExpeditionUTC is null || familiar.ExpeditionReturnUTC is null)
        {
            return new ExpeditionResult
            {
                FamiliarId = familiar.Id,
                DepartedUTC = nowUTC,
                ReturnedUTC = nowUTC,
                Duration = TimeSpan.Zero,
                Success = false
            };
        }

        var elapsed = nowUTC - familiar.LastExpeditionUTC.Value;
        if (elapsed > MaxExpeditionDuration)
            elapsed = MaxExpeditionDuration;

        var duration = elapsed;
        var success = familiar.CurrentHealth > 0;
        var effectiveRate = GetDiminishingReturnsRate(duration);
        var rarityMultiplier = GetRarityMultiplier(familiar.Rarity);

        var xp = (int)(duration.TotalSeconds * XpPerSecond * familiar.Level * rarityMultiplier * effectiveRate);
        var mana = (int)(duration.TotalSeconds * ManaPerSecond * familiar.GatheringBonus * rarityMultiplier * effectiveRate);

        var windows = (int)(duration.TotalSeconds / LootWindowSeconds);
        var loot = new List<LootDrop>();
        var rng = Random.Shared;

        for (int i = 0; i < windows; i++)
        {
            var adjustedChance = LootChancePerWindow * effectiveRate;
            if (rng.NextDouble() < adjustedChance)
                loot.Add(GenerateLootDrop(familiar));
        }

        return new ExpeditionResult
        {
            FamiliarId = familiar.Id,
            DepartedUTC = familiar.LastExpeditionUTC.Value,
            ReturnedUTC = nowUTC,
            Duration = duration,
            Success = success,
            ExperienceEarned = xp,
            ManaCrystalsEarned = mana,
            Loot = loot
        };
    }

    public static int CalculatePassiveMana(IEnumerable<SanctuaryBuilding> buildings, TimeSpan elapsed)
    {
        var totalMps = buildings.Sum(b => b.ManaPerSecond * b.Level);
        var effectiveRate = GetDiminishingReturnsRate(elapsed);
        return (int)(totalMps * elapsed.TotalSeconds * effectiveRate);
    }

    private static double GetRarityMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common => 1.0,
        Rarity.Uncommon => 1.3,
        Rarity.Rare => 1.7,
        Rarity.Epic => 2.2,
        Rarity.Legendary => 3.0,
        _ => 1.0
    };

    private static LootDrop GenerateLootDrop(Familiar familiar)
    {
        var rng = Random.Shared;
        var rarityRoll = rng.NextDouble();
        var dropRarity = rarityRoll switch
        {
            < 0.02 => Rarity.Legendary,
            < 0.08 => Rarity.Epic,
            < 0.22 => Rarity.Rare,
            < 0.50 => Rarity.Uncommon,
            _ => Rarity.Common
        };

        var elementNames = familiar.Element switch
        {
            ElementType.Mana => new[] { "Mana Shard", "Arcane Dust" },
            ElementType.Void => new[] { "Void Dust", "Entropy Fragment" },
            ElementType.Ember => new[] { "Ember Core", "Cinder Spark" },
            ElementType.Frost => new[] { "Frost Crystal", "Glacial Shard" },
            ElementType.Verdant => new[] { "Verdant Seed", "Life Essence" },
            ElementType.Luminous => new[] { "Light Prism", "Radiant Dust" },
            ElementType.Umbral => new[] { "Shadow Essence", "Night Residue" },
            _ => new[] { "Mysterious Fragment" }
        };

        return new LootDrop
        {
            ItemName = elementNames[rng.Next(elementNames.Length)],
            Element = familiar.Element,
            Rarity = dropRarity,
            Quantity = rng.Next(1, dropRarity switch
            {
                Rarity.Legendary => 3,
                Rarity.Epic => 5,
                Rarity.Rare => 8,
                _ => 15
            })
        };
    }
}
