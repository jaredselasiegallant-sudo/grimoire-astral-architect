using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// An egg that will hatch into a familiar after a real-time timer elapses.
/// Eggs are obtained from expeditions or narrative events.
/// </summary>
public sealed class FamiliarEgg
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The familiar type this egg will hatch into.</summary>
    public FamiliarType HatchesInto { get; init; }

    /// <summary>The element of the resulting familiar.</summary>
    public ElementType Element { get; init; }

    /// <summary>Rarity of the resulting familiar.</summary>
    public Rarity Rarity { get; init; } = Rarity.Common;

    /// <summary>Total real-time seconds required to hatch.</summary>
    public int HatchDurationSeconds { get; init; } = 300; // 5 minutes default

    /// <summary>UTC timestamp when incubation began.</summary>
    public DateTimeOffset? IncubationStartUTC { get; set; }

    /// <summary>UTC timestamp when the egg will be ready to open.</summary>
    public DateTimeOffset? HatchReadyUTC { get; set; }

    /// <summary>True while the egg is incubating.</summary>
    public bool IsIncubating { get; set; }

    /// <summary>True when the egg has hatched and the familiar is ready to name.</summary>
    public bool HasHatched { get; set; }

    /// <summary>Percentage complete (0-100) for UI display.</summary>
    public double HatchProgress
    {
        get
        {
            if (!IsIncubating || IncubationStartUTC is null) return 0;
            var elapsed = (DateTimeOffset.UtcNow - IncubationStartUTC.Value).TotalSeconds;
            return Math.Min(100, (elapsed / HatchDurationSeconds) * 100);
        }
    }

    /// <summary>Time remaining until hatch.</summary>
    public TimeSpan TimeRemaining
    {
        get
        {
            if (HatchReadyUTC is null) return TimeSpan.Zero;
            var remaining = HatchReadyUTC.Value - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
