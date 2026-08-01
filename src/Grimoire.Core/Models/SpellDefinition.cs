using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Models;

/// <summary>
/// A defined spell recipe: the gesture shape + required element + fuel cost.
/// </summary>
public sealed class SpellDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Description { get; set; }
    public SpellGesture RequiredGesture { get; init; }
    public ElementType Element { get; init; }

    /// <summary>Mana power cost to cast this spell.</summary>
    public int ManaCost { get; init; }

    /// <summary>Base power of the spell effect.</summary>
    public int BasePower { get; init; }

    /// <summary>True if this spell is currently unlocked by the player.</summary>
    public bool IsUnlocked { get; set; }
}
