namespace Grimoire.Core.Enums;

/// <summary>Personality traits a familiar can develop based on player interaction.</summary>
[Flags]
public enum FamiliarPersonality
{
    None = 0,
    Playful = 1,
    Curious = 2,
    Shy = 4,
    Brave = 8,
    Gentle = 16,
    Mischievous = 32,
    Wise = 64,
    Loyal = 128
}

/// <summary>How the familiar likes to be interacted with.</summary>
public enum BondingActivity
{
    Pet,           // Gentle stroking gesture
    Feed,          // Give favorite item
    Play,          // Circle gesture near them
    Rest,          // Leave them alone (some prefer this)
    Explore,       // Send on expedition
    Name           // Rename them
}
