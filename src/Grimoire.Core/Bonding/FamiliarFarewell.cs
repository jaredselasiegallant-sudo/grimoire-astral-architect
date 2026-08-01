using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Bonding;

/// <summary>
/// Familiar Farewells — a rare, optional system where a very old,
/// fully-bonded familiar can choose to "ascend" into a permanent
/// constellation fixture above the sanctuary.
/// 
/// A bittersweet, beautiful send-off that becomes a lasting monument
/// rather than a loss. Turns endgame into something people write about.
/// </summary>
public sealed class FamiliarFarewell
{
    /// <summary>Check if a familiar is eligible for ascension.</summary>
    public static bool IsEligible(Familiar familiar, FamiliarBond bond)
    {
        return familiar.Level >= 30 &&
               bond.BondLevel >= 9 &&
               bond.Happiness >= 80 &&
               bond.Trust >= 90;
    }

    /// <summary>Generate the ascension event for an eligible familiar.</summary>
    public static AscensionEvent CreateAscension(Familiar familiar, FamiliarBond bond)
    {
        return new AscensionEvent
        {
            FamiliarId = familiar.Id,
            FamiliarName = familiar.Name,
            FamiliarType = familiar.Type,
            Element = familiar.Element,
            FinalLevel = familiar.Level,
            FinalBondLevel = bond.BondLevel,
            TotalAffection = bond.Affection,
            Personality = bond.Personality,
            AscensionUTC = DateTimeOffset.UtcNow,
            ConstellationName = GenerateConstellationName(familiar),
            ConstellationDescription = GenerateConstellationDescription(familiar, bond),
            FarewellDialogue = GenerateFarewellDialogue(familiar, bond)
        };
    }

    private static string GenerateConstellationName(Familiar familiar) => familiar.Type switch
    {
        FamiliarType.Wisp => $"The {familiar.Name} Flame",
        FamiliarType.Sprite => $"The {familiar.Name} Dance",
        FamiliarType.Drakling => $"The {familiar.Name} Wing",
        FamiliarType.Mothwing => $"The {familiar.Name} Glow",
        FamiliarType.Golem => $"The {familiar.Name} Stone",
        FamiliarType.Shade => $"The {familiar.Name} Veil",
        FamiliarType.Foxfire => $"The {familiar.Name} Trail",
        _ => $"The Star of {familiar.Name}"
    };

    private static string GenerateConstellationDescription(Familiar familiar, FamiliarBond bond)
    {
        var trait = bond.Personality switch
        {
            FamiliarPersonality.Playful => "still dancing among the stars",
            FamiliarPersonality.Curious => "still exploring the spaces between",
            FamiliarPersonality.Gentle => "watching over all who sleep below",
            FamiliarPersonality.Brave => "standing guard at the edge of the Void",
            FamiliarPersonality.Wise => "whispering old knowledge to the wind",
            FamiliarPersonality.Loyal => "never truly leaving — just watching from further away",
            _ => "a steady light in the dark"
        };

        return $"A constellation of {familiar.Element} light, {trait}. " +
               $"Those who look up on clear nights say they can still feel its warmth.";
    }

    private static List<string> GenerateFarewellDialogue(Familiar familiar, FamiliarBond bond)
    {
        var dialogues = new List<string>
        {
            "The familiar circles you one last time, glow brighter than it has ever been.",
            "It presses its warmth against your palm — a touch that says everything words cannot.",
            "A single, clear note rings out. The sky opens. Light pours upward.",
            "You watch it rise — small, bright, certain — until it finds its place among the stars.",
            "The sanctuary feels different now. Smaller, somehow. But also larger, because the sky is closer.",
            "Other familiars look up. They don't seem sad. They seem... inspired."
        };

        if (bond.UnlockedDialogues.Count > 5)
            dialogues.Add("The last thing it showed you was a memory: the moment you first named it.");

        return dialogues;
    }
}

public sealed class AscensionEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FamiliarId { get; init; }
    public required string FamiliarName { get; init; }
    public FamiliarType FamiliarType { get; init; }
    public ElementType Element { get; init; }
    public int FinalLevel { get; init; }
    public int FinalBondLevel { get; init; }
    public int TotalAffection { get; init; }
    public FamiliarPersonality Personality { get; init; }
    public DateTimeOffset AscensionUTC { get; init; }
    public required string ConstellationName { get; init; }
    public required string ConstellationDescription { get; init; }
    public List<string> FarewellDialogue { get; init; } = [];
}

/// <summary>
/// Tracks constellations visible above the sanctuary from ascended familiars.
/// </summary>
public sealed class SanctuaryConstellations
{
    public List<Constellation> Stars { get; set; } = [];

    public void AddStar(AscensionEvent ascension)
    {
        Stars.Add(new Constellation
        {
            Name = ascension.ConstellationName,
            Description = ascension.ConstellationDescription,
            Element = ascension.Element,
            AscendedUTC = ascension.AscensionUTC,
            OriginalFamiliarName = ascension.FamiliarName
        });
    }
}

public sealed class Constellation
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public ElementType Element { get; init; }
    public DateTimeOffset AscendedUTC { get; init; }
    public required string OriginalFamiliarName { get; init; }
}
