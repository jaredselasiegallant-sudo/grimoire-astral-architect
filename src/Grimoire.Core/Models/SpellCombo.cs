using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A chained spell combo: multiple gestures cast in rapid succession
/// produce amplified or hybrid effects.
/// </summary>
public sealed class SpellCombo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<SpellGesture> GestureSequence { get; init; } = [];
    public ComboTier Tier { get; init; }
    public int ManaCost { get; init; }
    public int BasePower { get; init; }
    public float CooldownSeconds { get; init; }
    public string? RequiredUnlockedSpell { get; init; }
}

/// <summary>
/// Tracks the current combo chain state during active gameplay.
/// </summary>
public sealed class ComboTracker
{
    private readonly List<SpellGesture> _currentChain = [];
    private DateTimeOffset _chainStart;
    private const float MaxChainWindowSeconds = 8f;

    /// <summary>Current gestures in the chain.</summary>
    public IReadOnlyList<SpellGesture> CurrentChain => _currentChain;

    /// <summary>Whether a chain is active.</summary>
    public bool IsChaining => _currentChain.Count > 0 && TimeSinceLastGesture.TotalSeconds < MaxChainWindowSeconds;

    /// <summary>Time since the last gesture was added.</summary>
    public TimeSpan TimeSinceLastGesture { get; private set; }

    /// <summary>Current combo tier based on chain length.</summary>
    public ComboTier CurrentTier => _currentChain.Count switch
    {
        0 => ComboTier.None,
        1 => ComboTier.Basic,
        2 => ComboTier.Chained,
        3 => ComboTier.Ascended,
        _ => ComboTier.Transcendent
    };

    /// <summary>Add a gesture to the current chain. Returns the matched combo if any.</summary>
    public SpellCombo? AddGesture(SpellGesture gesture)
    {
        var now = DateTimeOffset.UtcNow;

        // Reset chain if window expired
        if (_currentChain.Count > 0 && (now - _chainStart).TotalSeconds > MaxChainWindowSeconds)
        {
            _currentChain.Clear();
        }

        if (_currentChain.Count == 0)
            _chainStart = now;

        _currentChain.Add(gesture);
        TimeSinceLastGesture = now - _chainStart;

        // Check against known combos
        return MatchCombo();
    }

    /// <summary>Clear the current chain.</summary>
    public void Clear()
    {
        _currentChain.Clear();
    }

    private SpellCombo? MatchCombo()
    {
        foreach (var combo in KnownCombos.All)
        {
            if (combo.GestureSequence.Count > _currentChain.Count) continue;

            // Check if the chain ends with this combo's sequence
            var startIdx = _currentChain.Count - combo.GestureSequence.Count;
            bool match = true;
            for (int i = 0; i < combo.GestureSequence.Count; i++)
            {
                if (_currentChain[startIdx + i] != combo.GestureSequence[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                _currentChain.Clear();
                return combo;
            }
        }
        return null;
    }
}

/// <summary>All known spell combos in the game.</summary>
public static class KnownCombos
{
    public static readonly List<SpellCombo> All =
    [
        // Circle + Circle = Greater Ward (2x defense)
        new()
        {
            Name = "Greater Ward",
            Description = "Two circles reinforce each other into an impenetrable barrier.",
            GestureSequence = [SpellGesture.Circle, SpellGesture.Circle],
            Tier = ComboTier.Chained,
            ManaCost = 18,
            BasePower = 50,
            CooldownSeconds = 15f
        },

        // Triangle + Line = Piercing Strike
        new()
        {
            Name = "Piercing Strike",
            Description = "The triangle focuses power, the line directs it with precision.",
            GestureSequence = [SpellGesture.Triangle, SpellGesture.Line],
            Tier = ComboTier.Chained,
            ManaCost = 22,
            BasePower = 60,
            CooldownSeconds = 12f
        },

        // Circle + Triangle + Line = Triforce of Binding
        new()
        {
            Name = "Triforce of Binding",
            Description = "Three shapes combine into the most powerful binding spell known.",
            GestureSequence = [SpellGesture.Circle, SpellGesture.Triangle, SpellGesture.Line],
            Tier = ComboTier.Ascended,
            ManaCost = 40,
            BasePower = 100,
            CooldownSeconds = 30f
        },

        // Spiral + Circle = Vortex Shield
        new()
        {
            Name = "Vortex Shield",
            Description = "The spiral pulls energy inward, the circle contains it.",
            GestureSequence = [SpellGesture.Spiral, SpellGesture.Circle],
            Tier = ComboTier.Chained,
            ManaCost = 35,
            BasePower = 70,
            CooldownSeconds = 20f
        },

        // Zigzag + Zigzag = Chain Lightning
        new()
        {
            Name = "Chain Lightning",
            Description = "Disruption layered upon disruption creates cascading energy.",
            GestureSequence = [SpellGesture.Zigzag, SpellGesture.Zigzag],
            Tier = ComboTier.Chained,
            ManaCost = 28,
            BasePower = 65,
            CooldownSeconds = 14f
        },

        // Spiral + Triangle + Circle + Line = Astral Convergence (ultimate)
        new()
        {
            Name = "Astral Convergence",
            Description = "The four cardinal shapes unite to reshape reality itself.",
            GestureSequence = [SpellGesture.Spiral, SpellGesture.Triangle, SpellGesture.Circle, SpellGesture.Line],
            Tier = ComboTier.Transcendent,
            ManaCost = 80,
            BasePower = 200,
            CooldownSeconds = 60f
        }
    ];
}
