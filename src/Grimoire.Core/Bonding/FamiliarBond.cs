using Grimoire.Core.Enums;

namespace Grimoire.Core.Bonding;

/// <summary>
/// Tracks the emotional bond between a player and their familiar.
/// Personality develops based on how the player interacts with them.
/// </summary>
public sealed class FamiliarBond
{
    public Guid FamiliarId { get; init; }

    /// <summary>Overall happiness (0-100). Decays slowly if neglected.</summary>
    public int Happiness { get; set; } = 50;

    /// <summary>Trust level (0-100). Increases with consistent positive interactions.</summary>
    public int Trust { get; set; } = 20;

    /// <summary>Affection points. Earned through bonding activities.</summary>
    public int Affection { get; set; }

    /// <summary>Developed personality traits (flags — can have multiple).</summary>
    public FamiliarPersonality Personality { get; set; }

    /// <summary>Favorite activity (determines best bonding method).</summary>
    public BondingActivity PreferredActivity { get; set; }

    /// <summary>Number of positive interactions in current session.</summary>
    public int SessionInteractions { get; set; }

    /// <summary>UTC of last interaction.</summary>
    public DateTimeOffset? LastInteractionUTC { get; set; }

    /// <summary>Bond level (1-10). Unlocks personality dialogue at milestones.</summary>
    public int BondLevel => Math.Min(10, 1 + Affection / 100);

    /// <summary>Unlocked dialogue entries at each bond milestone.</summary>
    public List<string> UnlockedDialogues { get; set; } = [];

    /// <summary>
    /// Process a bonding activity and update stats.
    /// Returns the personality change and dialogue triggered.
    /// </summary>
    public BondingResult ProcessActivity(BondingActivity activity)
    {
        var result = new BondingResult { Activity = activity };
        var isPreferred = activity == PreferredActivity;

        // Happiness change
        var happinessDelta = activity switch
        {
            BondingActivity.Pet => isPreferred ? 8 : 5,
            BondingActivity.Feed => isPreferred ? 10 : 6,
            BondingActivity.Play => isPreferred ? 12 : 7,
            BondingActivity.Rest => 3,
            BondingActivity.Explore => isPreferred ? 6 : 4,
            BondingActivity.Name => 15,
            _ => 2
        };

        // Trust change
        var trustDelta = activity switch
        {
            BondingActivity.Pet => 3,
            BondingActivity.Feed => 4,
            BondingActivity.Play => 2,
            BondingActivity.Rest => 1,
            BondingActivity.Explore => 5,
            BondingActivity.Name => 8,
            _ => 1
        };

        // Diminishing returns for spamming the same activity
        if (SessionInteractions > 3)
        {
            happinessDelta = (int)(happinessDelta * 0.5);
            trustDelta = (int)(trustDelta * 0.5);
        }

        Happiness = Math.Clamp(Happiness + happinessDelta, 0, 100);
        Trust = Math.Clamp(Trust + trustDelta, 0, 100);
        Affection += happinessDelta + trustDelta;
        SessionInteractions++;
        LastInteractionUTC = DateTimeOffset.UtcNow;

        result.HappinessDelta = happinessDelta;
        result.TrustDelta = trustDelta;

        // Personality development
        var newTrait = DevelopPersonality(activity);
        if (newTrait != FamiliarPersonality.None)
        {
            Personality |= newTrait;
            result.PersonalityDeveloped = newTrait;
        }

        // Bond level milestones unlock dialogue
        var previousLevel = 1 + (Affection - happinessDelta - trustDelta) / 100;
        if (BondLevel > previousLevel)
        {
            result.BondLevelUp = BondLevel;
            result.NewDialogue = GetDialogueForLevel(BondLevel);
            if (result.NewDialogue is not null)
                UnlockedDialogues.Add(result.NewDialogue);
        }

        return result;
    }

    /// <summary>
    /// Happiness decay when neglected. Called on each save tick.
    /// </summary>
    public void DecayHappiness(int amount = 1)
    {
        Happiness = Math.Max(0, Happiness - amount);
        if (Happiness < 20)
            Trust = Math.Max(0, Trust - 1);
    }

    private FamiliarPersonality DevelopPersonality(BondingActivity activity) => activity switch
    {
        BondingActivity.Play when Random.Shared.NextDouble() < 0.3 => FamiliarPersonality.Playful,
        BondingActivity.Explore when Random.Shared.NextDouble() < 0.3 => FamiliarPersonality.Curious,
        BondingActivity.Pet when Random.Shared.NextDouble() < 0.3 => FamiliarPersonality.Gentle,
        BondingActivity.Feed when Random.Shared.NextDouble() < 0.3 => FamiliarPersonality.Loyal,
        BondingActivity.Rest when Random.Shared.NextDouble() < 0.3 => FamiliarPersonality.Shy,
        BondingActivity.Name when Random.Shared.NextDouble() < 0.5 => FamiliarPersonality.Wise,
        _ => FamiliarPersonality.None
    };

    private static string? GetDialogueForLevel(int level) => level switch
    {
        2 => "The familiar tilts its head at you, recognition flickering in its eyes.",
        3 => "A soft chirp — the first sound it makes that feels intentional.",
        4 => "It nudges your hand when you're idle, as if asking you to stay.",
        5 => "The familiar circles you twice, then settles at your feet. Trust.",
        6 => "A warm glow pulses from its chest when you approach. Joy.",
        7 => "It brings you a small, glowing pebble. A gift.",
        8 => "The familiar follows you without being called. Home.",
        9 => "When you're sad, it presses close and hums. Understanding.",
        10 => "It speaks — not in words, but you understand perfectly. 'You are my sky.'",
        _ => null
    };
}

public sealed class BondingResult
{
    public BondingActivity Activity { get; init; }
    public int HappinessDelta { get; set; }
    public int TrustDelta { get; set; }
    public FamiliarPersonality? PersonalityDeveloped { get; set; }
    public int? BondLevelUp { get; set; }
    public string? NewDialogue { get; set; }
}
