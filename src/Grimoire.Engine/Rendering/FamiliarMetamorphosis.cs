using SkiaSharp;
using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Engine.Rendering;

/// <summary>
/// Familiar Visual Metamorphosis — familiars physically reshape over time.
/// Instead of a level-up number popping, they grow new limbs, denser glow,
/// and new particle trails. Growth is something you SEE happen across weeks.
/// </summary>
public sealed class FamiliarMetamorphosis
{
    /// <summary>
    /// Get the metamorphosis stage for a familiar based on level and bond.
    /// Each stage changes the visual representation.
    /// </summary>
    public static MetamorphosisStage GetStage(Familiar familiar, int bondLevel)
    {
        var effectiveLevel = familiar.Level + bondLevel;

        return effectiveLevel switch
        {
            <= 3 => MetamorphosisStage.Seed,       // Small, flickering, uncertain glow
            <= 7 => MetamorphosisStage.Sprout,      // Slightly larger, steadier, simple shape
            <= 12 => MetamorphosisStage.Bloom,      // Full shape, defined features, color shift
            <= 20 => MetamorphosisStage.Radiant,    // Glowing aura, particle trail, eyes bright
            <= 30 => MetamorphosisStage.Ascendant,  // Floating particles, light corona, complex shape
            _ => MetamorphosisStage.Transcendent    // Ethereal, semi-transparent, constellation hints
        };
    }

    /// <summary>
    /// Render a familiar at its current metamorphosis stage.
    /// Each stage adds visual complexity.
    /// </summary>
    public static void RenderFamiliar(
        SKCanvas canvas, Familiar familiar, int bondLevel,
        float x, float y, double elapsed, SKPaint paint)
    {
        var stage = GetStage(familiar, bondLevel);
        var baseColor = GetElementColor(familiar.Element);
        var floatY = y + MathF.Sin((float)elapsed * 2f + familiar.GetHashCode() % 100) * 5f;

        switch (stage)
        {
            case MetamorphosisStage.Seed:
                RenderSeed(canvas, x, floatY, baseColor, paint, elapsed);
                break;
            case MetamorphosisStage.Sprout:
                RenderSprout(canvas, x, floatY, baseColor, paint, elapsed);
                break;
            case MetamorphosisStage.Bloom:
                RenderBloom(canvas, x, floatY, baseColor, paint, elapsed);
                break;
            case MetamorphosisStage.Radiant:
                RenderRadiant(canvas, x, floatY, baseColor, paint, elapsed);
                break;
            case MetamorphosisStage.Ascendant:
                RenderAscendant(canvas, x, floatY, baseColor, paint, elapsed);
                break;
            case MetamorphosisStage.Transcendent:
                RenderTranscendent(canvas, x, floatY, baseColor, paint, elapsed);
                break;
        }

        // Name tag at all stages
        using var textPaint = new SKPaint
        {
            Color = new SKColor(200, 220, 255, 180),
            TextSize = 11,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Segoe UI")
        };
        canvas.DrawText(familiar.Name, x - textPaint.MeasureText(familiar.Name) / 2, floatY + 24, textPaint);
    }

    // ─── Stage Renderers ─────────────────────────────────────────

    private static void RenderSeed(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Tiny, flickering glow
        var flicker = 0.5f + 0.3f * MathF.Sin((float)elapsed * 4f);
        paint.Color = new SKColor(color.Red, color.Green, color.Blue, (byte)(80 * flicker));
        canvas.DrawCircle(x, y, 6f, paint);

        paint.Color = color;
        canvas.DrawCircle(x, y, 3f, paint);

        // Two dim eyes
        paint.Color = new SKColor(255, 255, 255, (byte)(120 * flicker));
        canvas.DrawCircle(x - 1.5f, y - 1f, 0.8f, paint);
        canvas.DrawCircle(x + 1.5f, y - 1f, 0.8f, paint);
    }

    private static void RenderSprout(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Slightly larger, steadier
        paint.Color = new SKColor(color.Red, color.Green, color.Blue, 50);
        canvas.DrawCircle(x, y, 12f, paint);

        paint.Color = color;
        canvas.DrawCircle(x, y, 5f, paint);

        // Small limbs (two tiny lines)
        using var limbPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            StrokeCap = SKStrokeCap.Round,
            Color = color,
            IsAntialias = true
        };

        var limbAngle1 = (float)elapsed * 0.5f;
        var limbAngle2 = -(float)elapsed * 0.5f;
        canvas.DrawLine(x, y, x + MathF.Cos(limbAngle1) * 7, y + MathF.Sin(limbAngle1) * 7, limbPaint);
        canvas.DrawLine(x, y, x + MathF.Cos(limbAngle2) * 7, y + MathF.Sin(limbAngle2) * 7, limbPaint);

        // Eyes brighter
        paint.Color = new SKColor(255, 255, 255, 180);
        canvas.DrawCircle(x - 2, y - 1.5f, 1f, paint);
        canvas.DrawCircle(x + 2, y - 1.5f, 1f, paint);
    }

    private static void RenderBloom(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Full shape with color shift
        var hueShift = MathF.Sin((float)elapsed * 0.3f) * 20;
        var shiftedColor = ShiftHue(color, hueShift);

        paint.Color = new SKColor(shiftedColor.Red, shiftedColor.Green, shiftedColor.Blue, 60);
        canvas.DrawCircle(x, y, 16f, paint);

        paint.Color = shiftedColor;
        canvas.DrawCircle(x, y, 7f, paint);

        // Defined limbs with gentle motion
        using var limbPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round, Color = shiftedColor, IsAntialias = true };

        for (int i = 0; i < 4; i++)
        {
            var angle = (float)elapsed * 0.3f + i * MathF.PI / 2;
            var len = 8 + MathF.Sin((float)elapsed + i) * 2;
            canvas.DrawLine(x, y, x + MathF.Cos(angle) * len, y + MathF.Sin(angle) * len, limbPaint);
        }

        // Bright eyes
        paint.Color = new SKColor(255, 255, 255, 220);
        canvas.DrawCircle(x - 2.5f, y - 2f, 1.5f, paint);
        canvas.DrawCircle(x + 2.5f, y - 2f, 1.5f, paint);
    }

    private static void RenderRadiant(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Glowing aura + particle trail
        paint.Color = new SKColor(color.Red, color.Green, color.Blue, 30);
        canvas.DrawCircle(x, y, 22f, paint);

        paint.Color = new SKColor(color.Red, color.Green, color.Blue, 60);
        canvas.DrawCircle(x, y, 16f, paint);

        // Body with shimmer
        var shimmer = 0.7f + 0.3f * MathF.Sin((float)elapsed * 3f);
        paint.Color = new SKColor(color.Red, color.Green, color.Blue, (byte)(255 * shimmer));
        canvas.DrawCircle(x, y, 8f, paint);

        // Particle trail
        using var trailPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        for (int i = 0; i < 5; i++)
        {
            var trailX = x + MathF.Sin((float)elapsed * 2 + i * 0.5f) * 3;
            var trailY = y + 10 + i * 3;
            var alpha = (byte)(150 - i * 30);
            trailPaint.Color = new SKColor(color.Red, color.Green, color.Blue, alpha);
            canvas.DrawCircle(trailX, trailY, 1.5f, trailPaint);
        }

        // Eyes with glow
        paint.Color = new SKColor(255, 255, 255, 240);
        canvas.DrawCircle(x - 3, y - 2, 1.5f, paint);
        canvas.DrawCircle(x + 3, y - 2, 1.5f, paint);
    }

    private static void RenderAscendant(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Light corona + floating particles
        using var coronaPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        // Outer corona
        coronaPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 20);
        canvas.DrawCircle(x, y, 28f, coronaPaint);

        // Inner corona
        coronaPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 40);
        canvas.DrawCircle(x, y, 20f, coronaPaint);

        // Body
        paint.Color = color;
        canvas.DrawCircle(x, y, 9f, paint);

        // Floating particles orbiting
        for (int i = 0; i < 8; i++)
        {
            var angle = (float)elapsed * 0.8f + i * MathF.PI * 2 / 8;
            var orbitRadius = 18 + MathF.Sin((float)elapsed + i) * 4;
            var px = x + MathF.Cos(angle) * orbitRadius;
            var py = y + MathF.Sin(angle) * orbitRadius;
            coronaPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 120);
            canvas.DrawCircle(px, py, 1.5f, coronaPaint);
        }

        // Eyes
        paint.Color = new SKColor(255, 255, 255, 255);
        canvas.DrawCircle(x - 3, y - 2, 2f, paint);
        canvas.DrawCircle(x + 3, y - 2, 2f, paint);
    }

    private static void RenderTranscendent(SKCanvas canvas, float x, float y, SKColor color, SKPaint paint, double elapsed)
    {
        // Ethereal, semi-transparent, constellation hints
        using var etherealPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        // Large ethereal glow
        etherealPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 15);
        canvas.DrawCircle(x, y, 35f, etherealPaint);

        etherealPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 30);
        canvas.DrawCircle(x, y, 25f, etherealPaint);

        // Semi-transparent body
        var alpha = (byte)(180 + 40 * MathF.Sin((float)elapsed * 1.5f));
        paint.Color = new SKColor(color.Red, color.Green, color.Blue, alpha);
        canvas.DrawCircle(x, y, 10f, paint);

        // Constellation lines connecting orbiting particles
        using var linePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 0.5f,
            Color = new SKColor(color.Red, color.Green, color.Blue, 60),
            IsAntialias = true
        };

        var points = new SKPoint[6];
        for (int i = 0; i < 6; i++)
        {
            var angle = (float)elapsed * 0.4f + i * MathF.PI * 2 / 6;
            var radius = 22 + MathF.Sin((float)elapsed * 0.7f + i) * 5;
            points[i] = new SKPoint(x + MathF.Cos(angle) * radius, y + MathF.Sin(angle) * radius);
            etherealPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 100);
            canvas.DrawCircle(points[i].X, points[i].Y, 1.5f, etherealPaint);
        }

        // Connect constellation
        for (int i = 0; i < points.Length; i++)
            canvas.DrawLine(points[i], points[(i + 1) % points.Length], linePaint);

        // Bright eyes
        paint.Color = new SKColor(255, 255, 255, 255);
        canvas.DrawCircle(x - 3, y - 2, 2f, paint);
        canvas.DrawCircle(x + 3, y - 2, 2f, paint);
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private static SKColor GetElementColor(ElementType element) => element switch
    {
        ElementType.Mana => new SKColor(100, 200, 255),
        ElementType.Void => new SKColor(160, 100, 255),
        ElementType.Ember => new SKColor(255, 120, 60),
        ElementType.Frost => new SKColor(140, 220, 255),
        ElementType.Verdant => new SKColor(100, 255, 140),
        ElementType.Luminous => new SKColor(255, 230, 100),
        ElementType.Umbral => new SKColor(120, 80, 200),
        _ => new SKColor(200, 200, 200)
    };

    private static SKColor ShiftHue(SKColor color, float degrees)
    {
        // Simple hue shift via RGB rotation
        var r = color.Red;
        var g = color.Green;
        var b = color.Blue;

        if (degrees > 0)
        {
            r = (byte)Math.Clamp(r + degrees * 0.5f, 0, 255);
            b = (byte)Math.Clamp(b - degrees * 0.3f, 0, 255);
        }
        else
        {
            g = (byte)Math.Clamp(g - degrees * 0.3f, 0, 255);
            b = (byte)Math.Clamp(b + degrees * 0.2f, 0, 255);
        }

        return new SKColor(r, g, b);
    }
}

public enum MetamorphosisStage
{
    Seed,        // Level 1-3
    Sprout,      // Level 4-7
    Bloom,       // Level 8-12
    Radiant,     // Level 13-20
    Ascendant,   // Level 21-30
    Transcendent // Level 31+
}
