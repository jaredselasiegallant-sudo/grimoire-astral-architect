using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Buildings;

/// <summary>
/// Architectural Rituals — certain building layouts silently unlock
/// hidden combo effects the game never explicitly lists.
/// Discovery-driven, screenshot-bait moments that spread by word of mouth.
/// </summary>
public static class ArchitecturalRituals
{
    private static readonly List<RitualPattern> Patterns =
    [
        // Three shrines in a triangle
        new()
        {
            Name = "Triangle of Light",
            Description = "Three Mana Shrines arranged in a triangle create a permanent mana wellspring.",
            RequiredBuildings = [(BuildingType.ManaShrine, 3)],
            Shape = RitualShape.Triangle,
            Effect = new RitualEffect { ManaMultiplier = 1.5f, Description = "Triple shrine resonance" },
            DiscoveredMessage = "The three shrines hum in unison — something ancient awakens."
        },

        // Habitat ringed by potion stations
        new()
        {
            Name = "Brewer's Circle",
            Description = "A Familiar Habitat surrounded by 4+ Potion Stations creates a healing aura.",
            RequiredBuildings = [(BuildingType.FamiliarHabitat, 1), (BuildingType.PotionStation, 4)],
            Shape = RitualShape.Ring,
            Effect = new RitualEffect { FamiliarHealRate = 2.0f, Description = "Potion healing circle" },
            DiscoveredMessage = "The potions orbit the habitat like moons — your familiars smile."
        },

        // Cauldron + Shrine + Obelisk in a line
        new()
        {
            Name = "Mana Conduit",
            Description = "Cauldron, Shrine, and Obelisk in a straight line create a mana pipeline.",
            RequiredBuildings = [(BuildingType.AlchemicalCauldron, 1), (BuildingType.ManaShrine, 1), (BuildingType.StarlightObelisk, 1)],
            Shape = RitualShape.Line,
            Effect = new RitualEffect { ManaMultiplier = 2.0f, CraftingPower = 1.5f, Description = "Mana pipeline" },
            DiscoveredMessage = "Energy flows visibly between the three structures — a conduit of pure mana."
        },

        // Void Anchor at center of 4+ buildings
        new()
        {
            Name = "Void Ward",
            Description = "A Void Anchor at the center of 4+ buildings creates a corruption shield.",
            RequiredBuildings = [(BuildingType.VoidAnchor, 1)],
            Shape = RitualShape.Cross,
            MinSurroundingBuildings = 4,
            Effect = new RitualEffect { CorruptionResistance = 0.8f, Description = "Void corruption shield" },
            DiscoveredMessage = "The Void recoils — the anchor pulses with protective energy."
        },

        // Garden + Obelisk adjacent
        new()
        {
            Name = "Starlight Garden",
            Description = "A Garden of Whispers directly adjacent to a Starlight Obelisk blooms eternally.",
            RequiredBuildings = [(BuildingType.GardenOfWhispers, 1), (BuildingType.StarlightObelisk, 1)],
            Shape = RitualShape.Adjacent,
            Effect = new RitualEffect { GardenYield = 3.0f, Description = "Eternal bloom" },
            DiscoveredMessage = "The garden glows with captured starlight — it will never wither."
        },

        // All 7 building types present
        new()
        {
            Name = "The Complete Sanctuary",
            Description = "All seven building types create a self-sustaining ecosystem.",
            RequiredBuildings = [
                (BuildingType.ManaShrine, 1), (BuildingType.PotionStation, 1),
                (BuildingType.FamiliarHabitat, 1), (BuildingType.AlchemicalCauldron, 1),
                (BuildingType.StarlightObelisk, 1), (BuildingType.VoidAnchor, 1),
                (BuildingType.GardenOfWhispers, 1)
            ],
            Shape = RitualShape.Any,
            Effect = new RitualEffect
            {
                ManaMultiplier = 1.3f,
                CraftingPower = 1.3f,
                FamiliarHealRate = 1.5f,
                CorruptionResistance = 0.5f,
                GardenYield = 2.0f,
                Description = "Self-sustaining sanctuary"
            },
            DiscoveredMessage = "Every type of building stands together. The sanctuary breathes on its own."
        }
    ];

    /// <summary>
    /// Check all ritual patterns against current building layout.
    /// Returns newly discovered rituals.
    /// </summary>
    public static List<RitualDiscovery> CheckRituals(
        IReadOnlyList<SanctuaryBuilding> buildings,
        HashSet<string> previouslyDiscovered)
    {
        var discoveries = new List<RitualDiscovery>();

        foreach (var pattern in Patterns)
        {
            if (previouslyDiscovered.Contains(pattern.Name)) continue;
            if (MatchesPattern(pattern, buildings))
            {
                discoveries.Add(new RitualDiscovery
                {
                    Pattern = pattern,
                    DiscoveredUTC = DateTimeOffset.UtcNow
                });
            }
        }

        return discoveries;
    }

    private static bool MatchesPattern(RitualPattern pattern, IReadOnlyList<SanctuaryBuilding> buildings)
    {
        // Check required building counts
        foreach (var (type, count) in pattern.RequiredBuildings)
        {
            if (buildings.Count(b => b.Type == type) < count)
                return false;
        }

        // Check shape
        return pattern.Shape switch
        {
            RitualShape.Triangle => HasTriangle(buildings, pattern.RequiredBuildings[0].Item1),
            RitualShape.Line => HasLine(buildings),
            RitualShape.Ring => HasRing(buildings),
            RitualShape.Cross => HasCross(buildings),
            RitualShape.Adjacent => HasAdjacent(buildings, pattern.RequiredBuildings),
            RitualShape.Any => true,
            _ => true
        };
    }

    private static bool HasTriangle(IReadOnlyList<SanctuaryBuilding> buildings, BuildingType type)
    {
        var targets = buildings.Where(b => b.Type == type).ToList();
        if (targets.Count < 3) return false;

        // Check if any 3 form a roughly equilateral triangle
        for (int i = 0; i < targets.Count; i++)
            for (int j = i + 1; j < targets.Count; j++)
                for (int k = j + 1; k < targets.Count; k++)
                {
                    var d1 = Distance(targets[i], targets[j]);
                    var d2 = Distance(targets[j], targets[k]);
                    var d3 = Distance(targets[k], targets[i]);
                    var avg = (d1 + d2 + d3) / 3;
                    if (avg > 0 && Math.Abs(d1 - avg) / avg < 0.4f &&
                        Math.Abs(d2 - avg) / avg < 0.4f &&
                        Math.Abs(d3 - avg) / avg < 0.4f)
                        return true;
                }
        return false;
    }

    private static bool HasLine(IReadOnlyList<SanctuaryBuilding> buildings)
    {
        var types = buildings.Select(b => b.Type).Distinct().ToList();
        if (types.Count < 3) return false;

        // Check if any 3 buildings are roughly collinear
        var all = buildings.ToList();
        for (int i = 0; i < all.Count; i++)
            for (int j = i + 1; j < all.Count; j++)
                for (int k = j + 1; k < all.Count; k++)
                {
                    var area = Math.Abs(
                        (all[j].GridX - all[i].GridX) * (all[k].GridY - all[i].GridY) -
                        (all[k].GridX - all[i].GridX) * (all[j].GridY - all[i].GridY));
                    if (area <= 2) return true; // Nearly collinear
                }
        return false;
    }

    private static bool HasRing(IReadOnlyList<SanctuaryBuilding> buildings)
    {
        // A ring = one building surrounded by 4+ others
        foreach (var center in buildings)
        {
            var surrounding = buildings.Count(b =>
                b.Id != center.Id &&
                Math.Abs(b.GridX - center.GridX) <= 2 &&
                Math.Abs(b.GridY - center.GridY) <= 2);
            if (surrounding >= 4) return true;
        }
        return false;
    }

    private static bool HasCross(IReadOnlyList<SanctuaryBuilding> buildings)
    {
        foreach (var center in buildings)
        {
            var hasAbove = buildings.Any(b => b.GridX == center.GridX && b.GridY < center.GridY);
            var hasBelow = buildings.Any(b => b.GridX == center.GridX && b.GridY > center.GridY);
            var hasLeft = buildings.Any(b => b.GridY == center.GridY && b.GridX < center.GridX);
            var hasRight = buildings.Any(b => b.GridY == center.GridY && b.GridX > center.GridX);
            if (hasAbove && hasBelow && hasLeft && hasRight) return true;
        }
        return false;
    }

    private static bool HasAdjacent(IReadOnlyList<SanctuaryBuilding> buildings, List<(BuildingType, int)> required)
    {
        foreach (var b in buildings)
        {
            var adjacentTypes = buildings
                .Where(o => o.Id != b.Id && Math.Abs(o.GridX - b.GridX) <= 1 && Math.Abs(o.GridY - b.GridY) <= 1)
                .Select(o => o.Type)
                .ToList();

            bool allPresent = true;
            foreach (var (type, _) in required)
            {
                if (type == b.Type) continue;
                if (!adjacentTypes.Contains(type)) { allPresent = false; break; }
            }
            if (allPresent) return true;
        }
        return false;
    }

    private static float Distance(SanctuaryBuilding a, SanctuaryBuilding b) =>
        MathF.Sqrt((a.GridX - b.GridX) * (a.GridX - b.GridX) + (a.GridY - b.GridY) * (a.GridY - b.GridY));
}

public sealed class RitualPattern
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<(BuildingType Type, int Count)> RequiredBuildings { get; init; } = [];
    public RitualShape Shape { get; init; }
    public int MinSurroundingBuildings { get; init; }
    public required RitualEffect Effect { get; init; }
    public required string DiscoveredMessage { get; init; }
}

public sealed class RitualEffect
{
    public float ManaMultiplier { get; init; } = 1.0f;
    public float CraftingPower { get; init; } = 1.0f;
    public float FamiliarHealRate { get; init; } = 1.0f;
    public float CorruptionResistance { get; init; }
    public float GardenYield { get; init; } = 1.0f;
    public required string Description { get; init; }
}

public sealed class RitualDiscovery
{
    public required RitualPattern Pattern { get; init; }
    public DateTimeOffset DiscoveredUTC { get; init; }
}

public enum RitualShape
{
    Any,
    Triangle,
    Line,
    Ring,
    Cross,
    Adjacent
}
