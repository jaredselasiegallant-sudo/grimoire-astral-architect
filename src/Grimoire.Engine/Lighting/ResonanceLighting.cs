using SkiaSharp;
using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Engine.Lighting;

/// <summary>
/// Resonance Lighting — every light source in the sanctuary casts
/// dynamic shadows and color bleed onto neighboring buildings.
/// The whole base subtly shifts hue as you place things.
/// Nothing is ever static-lit.
/// </summary>
public sealed class ResonanceLighting
{
    private readonly List<LightSource> _lights = [];
    private float _ambientIntensity = 0.15f;

    /// <summary>Ambient light level (0-1). Affects overall brightness.</summary>
    public float AmbientIntensity
    {
        get => _ambientIntensity;
        set => _ambientIntensity = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Update light sources from current building and familiar positions.</summary>
    public void UpdateFromState(
        IReadOnlyList<SanctuaryBuilding> buildings,
        IReadOnlyList<Familiar> familiars,
        float cellWidth, float cellHeight)
    {
        _lights.Clear();

        // Buildings emit light based on type and level
        foreach (var b in buildings)
        {
            var cx = b.GridX * cellWidth + cellWidth / 2f;
            var cy = b.GridY * cellHeight + cellHeight / 2f;

            var (color, intensity, radius) = b.Type switch
            {
                BuildingType.ManaShrine => (new SKColor(100, 200, 255), 0.8f, 3.0f),
                BuildingType.PotionStation => (new SKColor(180, 100, 255), 0.5f, 2.0f),
                BuildingType.FamiliarHabitat => (new SKColor(100, 255, 160), 0.4f, 2.5f),
                BuildingType.AlchemicalCauldron => (new SKColor(200, 80, 180), 0.6f, 2.0f),
                BuildingType.StarlightObelisk => (new SKColor(255, 230, 100), 0.9f, 3.5f),
                BuildingType.VoidAnchor => (new SKColor(80, 60, 200), 0.3f, 1.5f),
                BuildingType.GardenOfWhispers => (new SKColor(120, 220, 120), 0.4f, 2.0f),
                _ => (new SKColor(150, 150, 150), 0.3f, 1.5f)
            };

            // Level scales intensity
            var levelMultiplier = 1.0f + (b.Level - 1) * 0.1f;

            _lights.Add(new LightSource
            {
                X = cx,
                Y = cy,
                Color = color,
                Intensity = intensity * levelMultiplier,
                Radius = radius * cellWidth,
                Type = LightType.Building
            });
        }

        // Familiars emit soft glow
        foreach (var f in familiars.Where(f => !f.IsOnExpedition))
        {
            var fx = 8 * cellWidth + cellWidth / 2f;
            var fy = 5 * cellHeight + cellHeight / 2f;

            var color = f.Element switch
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

            _lights.Add(new LightSource
            {
                X = fx,
                Y = fy,
                Color = color,
                Intensity = 0.4f + f.Level * 0.05f,
                Radius = 2.0f * cellWidth,
                Type = LightType.Familiar
            });
        }
    }

    /// <summary>
    /// Render all resonance lighting effects.
    /// Each light bleeds its color onto nearby surfaces.
    /// </summary>
    public void Render(SKCanvas canvas, int width, int height)
    {
        using var bleedPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            BlendMode = SKBlendMode.Screen
        };

        foreach (var light in _lights)
        {
            // Color bleed: radial gradient from light color to transparent
            var alpha = (byte)(light.Intensity * 255 * _ambientIntensity);
            if (alpha < 5) continue;

            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(light.X, light.Y),
                light.Radius,
                [
                    new SKColor(light.Color.Red, light.Color.Green, light.Color.Blue, alpha),
                    new SKColor(light.Color.Red, light.Color.Green, light.Color.Blue, (byte)(alpha / 4)),
                    new SKColor(light.Color.Red, light.Color.Green, light.Color.Blue, 0)
                ],
                [0f, 0.4f, 1f],
                SKShaderTileMode.Clamp);

            bleedPaint.Shader = shader;
            canvas.DrawRect(0, 0, width, height, bleedPaint);
        }
    }

    /// <summary>
    /// Get the blended light colour at a specific pixel position.
    /// Used for dynamic element colouring of nearby objects.
    /// </summary>
    public SKColor GetLightAt(float x, float y)
    {
        float r = 0, g = 0, b = 0;

        foreach (var light in _lights)
        {
            var dist = MathF.Sqrt((x - light.X) * (x - light.X) + (y - light.Y) * (y - light.Y));
            if (dist > light.Radius) continue;

            var falloff = 1f - (dist / light.Radius);
            var contribution = falloff * falloff * light.Intensity;

            r += light.Color.Red * contribution;
            g += light.Color.Green * contribution;
            b += light.Color.Blue * contribution;
        }

        return new SKColor(
            (byte)Math.Clamp(r, 0, 255),
            (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255));
    }
}

public sealed class LightSource
{
    public float X { get; init; }
    public float Y { get; init; }
    public SKColor Color { get; init; }
    public float Intensity { get; init; }
    public float Radius { get; init; }
    public LightType Type { get; init; }
}

public enum LightType
{
    Building,
    Familiar,
    Spell,
    Particle
}
