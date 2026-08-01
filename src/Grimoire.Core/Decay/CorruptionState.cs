using Grimoire.Core.Enums;

namespace Grimoire.Core.Decay;

/// <summary>
/// Tracks Void corruption level across the sanctuary.
/// Neglected familiars and abandoned buildings slowly accumulate decay.
/// Never punishing — just a gentle visual tension that rewards engagement.
/// </summary>
public sealed class CorruptionState
{
    /// <summary>Current corruption level (0-100). 0 = pristine, 100 = heavily frayed.</summary>
    public int CorruptionLevel { get; set; }

    /// <summary>Corruption rate per in-game hour (base).</summary>
    public double BaseDecayRate { get; set; } = 0.5;

    /// <summary>Number of neglected familiars (happiness below 20).</summary>
    public int NeglectedFamiliarCount { get; set; }

    /// <summary>Number of buildings without adjacency (isolated).</summary>
    public int IsolatedBuildingCount { get; set; }

    /// <summary>Number of Void Anchors (reduce decay).</summary>
    public int VoidAnchorCount { get; set; }

    /// <summary>Whether corruption is visible in the skybox.</summary>
    public bool IsVisuallyCorrupting => CorruptionLevel > 15;

    /// <summary>Whether corruption affects gameplay (reduced yields).</summary>
    public bool IsGameplayImpacting => CorruptionLevel > 50;

    /// <summary>
    /// Calculate corruption change for a time delta.
    /// Called every save tick and on launch.
    /// </summary>
    public CorruptionResult CalculateDecay(TimeSpan elapsed)
    {
        var hours = elapsed.TotalHours;
        var rate = BaseDecayRate;

        // More neglected familiars = faster decay
        rate += NeglectedFamiliarCount * 0.3;

        // Isolated buildings decay faster
        rate += IsolatedBuildingCount * 0.1;

        // Void Anchors reduce decay (stacking)
        rate *= Math.Max(0.1, 1.0 - VoidAnchorCount * 0.15);

        // Corruption itself accelerates decay (feedback loop, but capped)
        rate *= 1.0 + CorruptionLevel * 0.005;

        var delta = (int)(rate * hours);
        var previousLevel = CorruptionLevel;
        CorruptionLevel = Math.Clamp(CorruptionLevel + delta, 0, 100);

        return new CorruptionResult
        {
            PreviousLevel = previousLevel,
            NewLevel = CorruptionLevel,
            Delta = delta,
            HoursElapsed = hours,
            IsVisuallyChanged = (previousLevel / 15) != (CorruptionLevel / 15)
        };
    }

    /// <summary>
    /// Reduce corruption through active play.
    /// Building a Void Anchor, completing expeditions, and bonding with familiars reduces decay.
    /// </summary>
    public void ReduceCorruption(int amount)
    {
        CorruptionLevel = Math.Max(0, CorruptionLevel - amount);
    }

    /// <summary>
    /// Get the visual corruption intensity (0-1) for rendering.
    /// </summary>
    public float GetVisualIntensity() => CorruptionLevel / 100f;

    /// <summary>
    /// Get the skybox corruption tint colour.
    /// </summary>
    public (byte R, byte G, byte B) GetCorruptionTint()
    {
        // corruption adds a greenish-purple haze
        var intensity = GetVisualIntensity();
        return (
            R: (byte)(40 * intensity),
            G: (byte)(20 * intensity),
            B: (byte)(60 * intensity)
        );
    }
}

public sealed class CorruptionResult
{
    public int PreviousLevel { get; init; }
    public int NewLevel { get; init; }
    public int Delta { get; init; }
    public double HoursElapsed { get; init; }
    public bool IsVisuallyChanged { get; init; }
}
