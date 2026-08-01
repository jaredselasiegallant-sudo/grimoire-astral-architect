using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Models;

/// <summary>
/// Duet Casting — certain powerful spells require two familiars
/// responding to the same gesture simultaneously, their two "voices" harmonizing.
/// Turns familiar pairing into an active puzzle.
/// </summary>
public sealed class DuetSpell
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public SpellGesture RequiredGesture { get; init; }

    /// <summary>The two element types required for the duet.</summary>
    public ElementType RequiredElementA { get; init; }
    public ElementType RequiredElementB { get; init; }

    public int ManaCost { get; init; }
    public int BasePower { get; init; }
    public float CooldownSeconds { get; init; }

    /// <summary>Narrative text when this duet is successfully cast.</summary>
    public required string CastNarrative { get; init; }
}

/// <summary>
/// All known duet spells in the game.
/// </summary>
public static class DuetSpellbook
{
    public static readonly List<DuetSpell> All =
    [
        new()
        {
            Name = "Convergence",
            Description = "Mana and Void energies spiral together into a beam of pure creation.",
            RequiredGesture = SpellGesture.Spiral,
            RequiredElementA = ElementType.Mana,
            RequiredElementB = ElementType.Void,
            ManaCost = 40,
            BasePower = 120,
            CooldownSeconds = 30f,
            CastNarrative = "Two voices become one. The air itself seems to hold its breath."
        },
        new()
        {
            Name = "Phoenix Bloom",
            Description = "Ember and Verdant energies merge — destruction and growth in perfect balance.",
            RequiredGesture = SpellGesture.Circle,
            RequiredElementA = ElementType.Ember,
            RequiredElementB = ElementType.Verdant,
            ManaCost = 35,
            BasePower = 100,
            CooldownSeconds = 25f,
            CastNarrative = "Fire and leaf dance together. Where they touch, new things are born."
        },
        new()
        {
            Name = "Frozen Starlight",
            Description = "Frost and Luminous energies crystallize into a shard of captured starlight.",
            RequiredGesture = SpellGesture.Triangle,
            RequiredElementA = ElementType.Frost,
            RequiredElementB = ElementType.Luminous,
            ManaCost = 35,
            BasePower = 100,
            CooldownSeconds = 25f,
            CastNarrative = "Light freezes mid-fall. For a perfect moment, time itself is visible."
        },
        new()
        {
            Name = "Shadow Weave",
            Description = "Umbral and Mana energies interweave, creating a cloak of invisible power.",
            RequiredGesture = SpellGesture.Zigzag,
            RequiredElementA = ElementType.Umbral,
            RequiredElementB = ElementType.Mana,
            ManaCost = 45,
            BasePower = 130,
            CooldownSeconds = 35f,
            CastNarrative = "Darkness and light braid together. The boundary between seen and unseen dissolves."
        }
    ];

    /// <summary>Check if two familiars can cast a specific duet spell.</summary>
    public static bool CanCast(DuetSpell spell, Familiar a, Familiar b)
    {
        var elements = new[] { a.Element, b.Element };
        return elements.Contains(spell.RequiredElementA) && elements.Contains(spell.RequiredElementB);
    }

    /// <summary>Find all duet spells available with two specific familiars.</summary>
    public static List<DuetSpell> GetAvailable(Familiar a, Familiar b) =>
        All.Where(s => CanCast(s, a, b)).ToList();
}
