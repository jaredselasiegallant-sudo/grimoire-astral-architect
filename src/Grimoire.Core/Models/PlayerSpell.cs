using System.Numerics;
using Grimoire.Core.Enums;

namespace Grimoire.Core.Models;

/// <summary>
/// A spell shape drawn and named by the player.
/// The game fuzzy-matches the drawn shape to an archetype,
/// letting advanced players create custom spells.
/// </summary>
public sealed class PlayerSpell
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }

    /// <summary>Reference points that define the drawn shape (normalised 0-1 coordinates).</summary>
    public List<Vector2> ReferenceShape { get; init; } = [];

    /// <summary>The archetype this shape maps to (determined by fuzzy matching).</summary>
    public SpellArchetype DetectedArchetype { get; set; }

    /// <summary>Custom power modifier (set by the game based on shape quality).</summary>
    public float PowerModifier { get; set; } = 1.0f;

    /// <summary>Elemental affinity of the spell (determined by player's dominant element).</summary>
    public ElementType Element { get; init; }

    /// <summary>Number of times this spell has been successfully cast.</summary>
    public int CastCount { get; set; }

    /// <summary>UTC when this spell was created.</summary>
    public DateTimeOffset CreatedUTC { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this spell is currently equipped for casting.</summary>
    public bool IsEquipped { get; set; }
}

/// <summary>
/// Analyses a drawn gesture and determines which SpellArchetype it maps to.
/// Uses simplified shape analysis: enclosing shapes → Ward, directional → Bolt, etc.
/// </summary>
public static class PlayerSpellAnalyser
{
    /// <summary>
    /// Analyse a normalised set of points and determine the archetype.
    /// </summary>
    public static SpellArchetype AnalyseShape(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 5) return SpellArchetype.Bolt;

        var closedness = Vector2.Distance(points[0], points[^1]);
        var avgDistanceFromCentroid = GetAverageDistanceFromCentroid(points);
        var straightness = GetStraightness(points);
        var enclosedArea = GetEnclosedArea(points);

        // Enclosing shapes → Ward
        if (closedness < 0.3f && enclosedArea > 0.02f)
            return SpellArchetype.Ward;

        // Long, straight → Bolt
        if (straightness > 0.8f)
            return SpellArchetype.Bolt;

        // Curvy, inward-spiraling → Heal
        if (avgDistanceFromCentroid < 0.15f && IsInwardSpiral(points))
            return SpellArchetype.Heal;

        // Many direction changes → Enchant
        if (GetDirectionChanges(points) > 6)
            return SpellArchetype.Enchant;

        // Dispersing outward → Summon
        if (IsOutwardExpanding(points))
            return SpellArchetype.Summon;

        // Default fallback
        return SpellArchetype.Dispel;
    }

    /// <summary>
    /// Calculate a power modifier based on shape quality.
    /// More symmetrical, smoother shapes get higher modifiers.
    /// </summary>
    public static float CalculatePowerModifier(IReadOnlyList<Vector2> points)
    {
        var symmetry = GetSymmetryScore(points);
        var smoothness = GetSmoothnessScore(points);
        var size = GetEnclosedArea(points);

        return Math.Clamp(0.5f + (symmetry * 0.3f + smoothness * 0.3f + size * 0.4f), 0.5f, 2.0f);
    }

    private static float GetAverageDistanceFromCentroid(IReadOnlyList<Vector2> points)
    {
        var centroid = new Vector2(points.Average(p => p.X), points.Average(p => p.Y));
        return points.Average(p => Vector2.Distance(p, centroid));
    }

    private static float GetStraightness(IReadOnlyList<Vector2> points)
    {
        var chord = Vector2.Distance(points[0], points[^1]);
        var pathLen = 0f;
        for (int i = 1; i < points.Count; i++)
            pathLen += Vector2.Distance(points[i - 1], points[i]);
        return pathLen > 0 ? chord / pathLen : 0;
    }

    private static float GetEnclosedArea(IReadOnlyList<Vector2> points)
    {
        // Shoelace formula
        float area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            int j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        return Math.Abs(area) / 2f;
    }

    private static bool IsInwardSpiral(IReadOnlyList<Vector2> points)
    {
        var centroid = new Vector2(points.Average(p => p.X), points.Average(p => p.Y));
        var firstHalf = points.Take(points.Count / 2).Average(p => Vector2.Distance(p, centroid));
        var secondHalf = points.Skip(points.Count / 2).Average(p => Vector2.Distance(p, centroid));
        return secondHalf < firstHalf;
    }

    private static bool IsOutwardExpanding(IReadOnlyList<Vector2> points)
    {
        var centroid = new Vector2(points.Average(p => p.X), points.Average(p => p.Y));
        var firstHalf = points.Take(points.Count / 2).Average(p => Vector2.Distance(p, centroid));
        var secondHalf = points.Skip(points.Count / 2).Average(p => Vector2.Distance(p, centroid));
        return secondHalf > firstHalf * 1.3f;
    }

    private static int GetDirectionChanges(IReadOnlyList<Vector2> points)
    {
        int changes = 0;
        for (int i = 2; i < points.Count; i++)
        {
            var cross1 = (points[i - 1].X - points[i - 2].X) * (points[i].Y - points[i - 1].Y)
                        - (points[i - 1].Y - points[i - 2].Y) * (points[i].X - points[i - 1].X);
            var cross2 = (points[i - 2].X - (i >= 3 ? points[i - 3].X : points[i - 2].X))
                        * (points[i - 1].Y - (i >= 3 ? points[i - 3].Y : points[i - 2].Y))
                        - (points[i - 2].Y - (i >= 3 ? points[i - 3].Y : points[i - 2].Y))
                        * (points[i - 1].X - (i >= 3 ? points[i - 3].X : points[i - 2].X));

            if (i >= 3 && cross1 * cross2 < 0) changes++;
        }
        return changes;
    }

    private static float GetSymmetryScore(IReadOnlyList<Vector2> points)
    {
        // Simple horizontal symmetry check
        var centroidX = points.Average(p => p.X);
        float totalError = 0;
        int pairs = 0;

        for (int i = 0; i < points.Count / 2; i++)
        {
            var mirror = points[points.Count - 1 - i];
            var dist = Math.Abs(points[i].X - (2 * centroidX - mirror.X));
            totalError += dist;
            pairs++;
        }

        return pairs > 0 ? 1f - Math.Min(1f, totalError / pairs) : 0.5f;
    }

    private static float GetSmoothnessScore(IReadOnlyList<Vector2> points)
    {
        if (points.Count < 3) return 1f;

        float totalAngle = 0;
        for (int i = 1; i < points.Count - 1; i++)
        {
            var v1 = points[i] - points[i - 1];
            var v2 = points[i + 1] - points[i];
            var dot = Vector2.Dot(v1, v2) / (v1.Length() * v2.Length() + 0.0001f);
            totalAngle += MathF.Acos(Math.Clamp(dot, -1f, 1f));
        }

        var avgAngle = totalAngle / (points.Count - 2);
        return 1f - Math.Min(1f, avgAngle / MathF.PI);
    }
}
