using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Buildings;

/// <summary>
/// Calculates adjacency bonuses between sanctuary buildings.
/// Buildings placed next to each other provide synergy multipliers.
/// </summary>
public static class BuildingSynergyCalculator
{
    /// <summary>Synergy rules: (BuildingType A, BuildingType B) → bonus multiplier.</summary>
    private static readonly Dictionary<(BuildingType, BuildingType), SynergyRule> SynergyRules = new()
    {
        // Shrine + Habitat = familiars gather 20% more
        [(BuildingType.ManaShrine, BuildingType.FamiliarHabitat)] = new("Starlight Haven", "Familiars nearby draw strength from the shrine.", 0.20, SynergyType.GatheringBonus),
        [(BuildingType.ManaShrine, BuildingType.StarlightObelisk)] = new("Celestial Pillar", "Obelisk and shrine amplify each other.", 0.30, SynergyType.ManaPerSecond),
        [(BuildingType.AlchemicalCauldron, BuildingType.PotionStation)] = new("Alchemy Loop", "Crafting speed doubles when cauldron meets station.", 0.50, SynergyType.CraftingSpeed),
        [(BuildingType.FamiliarHabitat, BuildingType.GardenOfWhispers)] = new("Living Garden", "Familiars and garden sustain each other.", 0.25, SynergyType.HappinessBonus),
        [(BuildingType.VoidAnchor, BuildingType.ManaShrine)] = new("Stabilized Core", "Anchor protects the shrine from Void decay.", 0.40, SynergyType.DecayResistance),
        [(BuildingType.GardenOfWhispers, BuildingType.StarlightObelisk)] = new("Moonlit Garden", "Garden yields triple under obelisk light.", 0.30, SynergyType.GardenYield),
        [(BuildingType.AlchemicalCauldron, BuildingType.ManaShrine)] = new("Mana Infusion", "Cauldron draws raw mana for stronger brews.", 0.20, SynergyType.CraftingPower),
    };

    /// <summary>
    /// Calculate all synergies for a building at a given grid position.
    /// </summary>
    public static List<SynergyResult> CalculateSynergies(SanctuaryBuilding building, IReadOnlyList<SanctuaryBuilding> allBuildings)
    {
        var results = new List<SynergyResult>();

        foreach (var other in allBuildings)
        {
            if (other.Id == building.Id) continue;

            // Check adjacency (8-directional: within 1 grid cell)
            var dx = Math.Abs(building.GridX - other.GridX);
            var dy = Math.Abs(building.GridY - other.GridY);
            if (dx > 1 || dy > 1) continue;

            var key = (building.Type, other.Type);
            var reverseKey = (other.Type, building.Type);

            if (SynergyRules.TryGetValue(key, out var rule))
            {
                results.Add(new SynergyResult
                {
                    Rule = rule,
                    PartnerBuilding = other,
                    Direction = GetDirection(building, other)
                });
            }
            else if (SynergyRules.TryGetValue(reverseKey, out var reverseRule))
            {
                results.Add(new SynergyResult
                {
                    Rule = reverseRule,
                    PartnerBuilding = building,
                    Direction = GetDirection(other, building)
                });
            }
        }

        return results;
    }

    /// <summary>Calculate total mana per second with synergy bonuses.</summary>
    public static double CalculateTotalManaPerSecond(IReadOnlyList<SanctuaryBuilding> buildings)
    {
        var baseMana = buildings.Sum(b => b.ManaPerSecond * b.Level);
        var bonusMana = 0.0;

        foreach (var building in buildings)
        {
            var synergies = CalculateSynergies(building, buildings);
            foreach (var synergy in synergies)
            {
                if (synergy.Rule.Type == SynergyType.ManaPerSecond)
                    bonusMana += building.ManaPerSecond * building.Level * synergy.Rule.Multiplier;
            }
        }

        return baseMana + bonusMana;
    }

    private static string GetDirection(SanctuaryBuilding from, SanctuaryBuilding to)
    {
        var dx = to.GridX - from.GridX;
        var dy = to.GridY - from.GridY;
        return (dx, dy) switch
        {
            (0, -1) => "North",
            (1, -1) => "Northeast",
            (1, 0) => "East",
            (1, 1) => "Southeast",
            (0, 1) => "South",
            (-1, 1) => "Southwest",
            (-1, 0) => "West",
            (-1, -1) => "Northwest",
            _ => "Adjacent"
        };
    }
}

public sealed class SynergyRule
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public double Multiplier { get; init; }
    public SynergyType Type { get; init; }

    public SynergyRule(string name, string description, double multiplier, SynergyType type)
    {
        Name = name;
        Description = description;
        Multiplier = multiplier;
        Type = type;
    }
}

public sealed class SynergyResult
{
    public required SynergyRule Rule { get; init; }
    public required SanctuaryBuilding PartnerBuilding { get; init; }
    public required string Direction { get; init; }
}

public enum SynergyType
{
    ManaPerSecond,
    GatheringBonus,
    CraftingSpeed,
    CraftingPower,
    HappinessBonus,
    DecayResistance,
    GardenYield
}
