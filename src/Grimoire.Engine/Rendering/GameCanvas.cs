using SkiaSharp;
using Grimoire.Core.Enums;
using Grimoire.Core.Decay;
using Grimoire.Core.World;
using Grimoire.Core.Weather;
using Grimoire.Core.Bonding;
using Grimoire.Core.Audio;
using Grimoire.Engine.Lighting;
using Grimoire.Engine.Ecology;

namespace Grimoire.Engine.Rendering;

/// <summary>
/// Renders the game world with all visual systems:
/// seasonal skybox, corruption overlay, buildings, familiars,
/// particles, gesture trail, return ritual, and photo mode.
/// </summary>
public sealed class GameCanvas
{
    private readonly SKPaint _gridPaint;
    private readonly SKPaint _trailPaint;
    private readonly SKPaint _textPaint;

    private float _cellWidth;
    private float _cellHeight;
    private SKRect _canvasBounds;

    private readonly List<SKPoint> _gestureTrail = [];
    private byte _trailAlpha = 255;

    private readonly ResonanceLighting _lighting = new();
    private readonly FamiliarMetamorphosis _metamorphosis = new();
    private readonly ParticleEcology _ecology = new();

    private static readonly Dictionary<BuildingType, SKColor> BuildingColors = new()
    {
        [BuildingType.ManaShrine] = new SKColor(100, 200, 255),
        [BuildingType.PotionStation] = new SKColor(180, 100, 255),
        [BuildingType.FamiliarHabitat] = new SKColor(100, 255, 160),
        [BuildingType.AlchemicalCauldron] = new SKColor(200, 80, 180),
        [BuildingType.StarlightObelisk] = new SKColor(255, 230, 100),
        [BuildingType.VoidAnchor] = new SKColor(80, 60, 200),
        [BuildingType.GardenOfWhispers] = new SKColor(120, 220, 120)
    };

    public GameCanvas()
    {
        _gridPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f, Color = new SKColor(80, 120, 200, 30), IsAntialias = true };
        _trailPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 3f, IsAntialias = true, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
        _textPaint = new SKPaint { Color = new SKColor(200, 220, 255), TextSize = 12, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Segoe UI") };
    }

    /// <summary>Main render entry point.</summary>
    public void Render(SKCanvas canvas, SKSizeI size, double elapsedSeconds,
        IReadOnlyList<Core.Models.SanctuaryBuilding>? buildings = null,
        IReadOnlyList<Core.Models.Familiar>? familiars = null,
        CorruptionState? corruption = null,
        WeatherState? weather = null,
        SanctuaryConstellations? constellations = null)
    {
        _canvasBounds = new SKRect(0, 0, size.Width, size.Height);
        _cellWidth = size.Width / 16f;
        _cellHeight = size.Height / 10f;

        canvas.Clear(SKColors.Black);

        // Layer 1: Seasonal skybox
        DrawSeasonalSkybox(canvas, size);

        // Layer 2: Corruption overlay (if any)
        if (corruption is not null && corruption.IsVisuallyCorrupting)
            DrawCorruptionOverlay(canvas, size, corruption);

        // Layer 3: Grid
        DrawGrid(canvas);

        // Layer 3.5: Resonance Lighting
        _lighting.Render(canvas, size.Width, size.Height);

        // Layer 4: Buildings
        DrawBuildings(canvas, buildings);

        // Layer 5: Familiars
        DrawFamiliars(canvas, familiars, elapsedSeconds);

        // Layer 6: Gesture trail
        DrawGestureTrail(canvas);

        // Layer 7: Particle Ecology
        _ecology.Render(canvas);

        // Layer 8: Constellations (above everything except trail)
        if (constellations is not null)
            DrawConstellations(canvas, size, constellations, elapsedSeconds);

        // Layer 9: Weather effects
        if (weather is not null)
            DrawWeatherEffects(canvas, size, weather, elapsedSeconds);
    }

    // ─── Seasonal Skybox ─────────────────────────────────────────

    private void DrawSeasonalSkybox(SKCanvas canvas, SKSizeI size)
    {
        var palette = SeasonalSync.GetCurrentPalette();

        var topColor = SKColor.Parse(palette.SkyTopHex);
        var bottomColor = SKColor.Parse(palette.SkyBottomHex);

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(0, size.Height),
            [topColor, bottomColor], [0f, 1f], SKShaderTileMode.Clamp);
        canvas.DrawRect(_canvasBounds, new SKPaint { Shader = shader });

        // Stars during night
        var timeOfDay = SeasonalSync.GetCurrentTimeOfDay();
        if (timeOfDay is TimeOfDay.DeepNight or TimeOfDay.Night)
            DrawStars(canvas, size, palette.Brightness);

        // Seasonal accent particles
        DrawSeasonalAccents(canvas, size, palette);
    }

    private void DrawStars(SKCanvas canvas, SKSizeI size, float brightness)
    {
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        var rng = new Random(42);
        var starCount = (int)(80 * brightness);

        for (int i = 0; i < starCount; i++)
        {
            var x = rng.Next(0, size.Width);
            var y = rng.Next(0, (int)(size.Height * 0.6f));
            var radius = (float)(rng.NextDouble() * 1.5 + 0.3);
            var alpha = (byte)(80 + rng.Next(0, (int)(175 * brightness)));
            paint.Color = new SKColor(255, 255, 255, alpha);
            canvas.DrawCircle(x, y, radius, paint);
        }
    }

    private void DrawSeasonalAccents(SKCanvas canvas, SKSizeI size, SeasonalPalette palette)
    {
        var season = SeasonalSync.GetCurrentSeason();
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        var accentColor = SKColor.Parse(palette.AccentHex);
        var rng = new Random(DateTime.Now.DayOfYear);

        int particleCount = season switch
        {
            Season.Spring => 15,
            Season.Summer => 8,
            Season.Autumn => 20,
            Season.Winter => 25,
            _ => 10
        };

        for (int i = 0; i < particleCount; i++)
        {
            var x = (float)(rng.NextDouble() * size.Width);
            var y = (float)(rng.NextDouble() * size.Height);
            var radius = 1f + (float)rng.NextDouble() * 2f;
            var alpha = (byte)(40 + rng.Next(0, 60));

            paint.Color = new SKColor(accentColor.Red, accentColor.Green, accentColor.Blue, alpha);
            canvas.DrawCircle(x, y, radius, paint);
        }
    }

    // ─── Corruption Overlay ──────────────────────────────────────

    private void DrawCorruptionOverlay(SKCanvas canvas, SKSizeI size, CorruptionState corruption)
    {
        var intensity = corruption.GetVisualIntensity();
        var tint = corruption.GetCorruptionTint();

        // Fraying edges — darker at the borders
        using var edgePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Edge vignette that intensifies with corruption
        using var vignetteShader = SKShader.CreateRadialGradient(
            new SKPoint(size.Width / 2f, size.Height / 2f),
            Math.Max(size.Width, size.Height) * 0.5f,
            [new SKColor(0, 0, 0, 0), new SKColor(tint.R, tint.G, tint.B, (byte)(120 * intensity))],
            [0.5f, 1f],
            SKShaderTileMode.Clamp);

        edgePaint.Shader = vignetteShader;
        canvas.DrawRect(_canvasBounds, edgePaint);

        // Corruption tendrils at high levels
        if (intensity > 0.4f)
        {
            using var tendrilPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true,
                Color = new SKColor(tint.R, tint.G, tint.B, (byte)(60 * intensity))
            };

            var rng = new Random(123);
            int tendrilCount = (int)(intensity * 12);
            for (int i = 0; i < tendrilCount; i++)
            {
                float startX = rng.Next(0, size.Width);
                float startY = rng.Next(0, size.Height);

                using var path = new SKPath();
                path.MoveTo(startX, startY);

                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                for (int seg = 0; seg < 8; seg++)
                {
                    angle += (float)(rng.NextDouble() - 0.5) * 1.2f;
                    var len = 15f + (float)rng.NextDouble() * 25f;
                    startX += MathF.Cos(angle) * len;
                    startY += MathF.Sin(angle) * len;
                    path.LineTo(startX, startY);
                }

                canvas.DrawPath(path, tendrilPaint);
            }
        }
    }

    // ─── Grid ────────────────────────────────────────────────────

    private void DrawGrid(SKCanvas canvas)
    {
        for (int x = 0; x <= 16; x++)
            canvas.DrawLine(x * _cellWidth, 0, x * _cellWidth, _canvasBounds.Height, _gridPaint);
        for (int y = 0; y <= 10; y++)
            canvas.DrawLine(0, y * _cellHeight, _canvasBounds.Width, y * _cellHeight, _gridPaint);
    }

    // ─── Buildings ───────────────────────────────────────────────

    private void DrawBuildings(SKCanvas canvas, IReadOnlyList<Core.Models.SanctuaryBuilding>? buildings)
    {
        if (buildings is null) return;

        using var fillPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var glowPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        foreach (var b in buildings)
        {
            var color = BuildingColors.GetValueOrDefault(b.Type, new SKColor(100, 100, 100));
            var cx = b.GridX * _cellWidth + _cellWidth / 2f;
            var cy = b.GridY * _cellHeight + _cellHeight / 2f;
            var baseRadius = Math.Min(_cellWidth, _cellHeight) * 0.3f;

            // Outer glow
            glowPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 40);
            canvas.DrawCircle(cx, cy, baseRadius * 1.8f, glowPaint);

            // Building shape
            fillPaint.Color = color;
            if (b.Type == BuildingType.ManaShrine || b.Type == BuildingType.StarlightObelisk)
                DrawHexagon(canvas, cx, cy, baseRadius, fillPaint);
            else
                canvas.DrawCircle(cx, cy, baseRadius, fillPaint);

            // Level indicator
            if (b.Level > 1)
            {
                _textPaint.Color = new SKColor(255, 255, 255, 200);
                canvas.DrawText($"Lv{b.Level}", cx - 8, cy + 4, _textPaint);
            }
        }
    }

    private static void DrawHexagon(SKCanvas canvas, float cx, float cy, float radius, SKPaint paint)
    {
        using var path = new SKPath();
        for (int i = 0; i < 6; i++)
        {
            var angle = Math.PI / 180 * (60 * i - 30);
            var x = cx + radius * (float)Math.Cos(angle);
            var y = cy + radius * (float)Math.Sin(angle);
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }
        path.Close();
        canvas.DrawPath(path, paint);
    }

    // ─── Familiars ───────────────────────────────────────────────

    private void DrawFamiliars(SKCanvas canvas, IReadOnlyList<Core.Models.Familiar>? familiars, double elapsed)
    {
        if (familiars is null) return;
        using var paint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        foreach (var f in familiars)
        {
            if (f.IsOnExpedition) continue;

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

            var fx = 8 * _cellWidth + _cellWidth / 2f;
            var fy = 5 * _cellHeight + _cellHeight / 2f;
            var floatOffset = MathF.Sin((float)elapsed * 2f + f.GetHashCode() % 100) * 5f;

            // Glow
            paint.Color = new SKColor(color.Red, color.Green, color.Blue, 50);
            canvas.DrawCircle(fx, fy + floatOffset, 20f, paint);

            // Body
            paint.Color = color;
            canvas.DrawCircle(fx, fy + floatOffset, 8f, paint);

            // Eyes
            paint.Color = new SKColor(255, 255, 255, 220);
            canvas.DrawCircle(fx - 3, fy + floatOffset - 2, 1.5f, paint);
            canvas.DrawCircle(fx + 3, fy + floatOffset - 2, 1.5f, paint);

            // Name tag
            _textPaint.Color = new SKColor(200, 220, 255, 180);
            canvas.DrawText(f.Name, fx - _textPaint.MeasureText(f.Name) / 2, fy + floatOffset + 22, _textPaint);

            // Familiar Metamorphosis stage rendering
            FamiliarMetamorphosis.RenderFamiliar(canvas, f, 0, fx, fy + floatOffset, elapsed, _textPaint);
        }
    }

    // ─── Gesture Trail ───────────────────────────────────────────

    public void SetGestureTrail(IEnumerable<System.Numerics.Vector2> points)
    {
        _gestureTrail.Clear();
        _trailAlpha = 255;
        foreach (var p in points)
            _gestureTrail.Add(new SKPoint(p.X, p.Y));
    }

    public void ClearGestureTrail() => _gestureTrail.Clear();

    private void DrawGestureTrail(SKCanvas canvas)
    {
        if (_gestureTrail.Count < 2) return;

        _trailPaint.Color = new SKColor(100, 200, 255, _trailAlpha);
        _trailPaint.StrokeWidth = 3f;

        using var path = new SKPath();
        path.MoveTo(_gestureTrail[0]);
        for (int i = 1; i < _gestureTrail.Count; i++)
            path.LineTo(_gestureTrail[i]);
        canvas.DrawPath(path, _trailPaint);

        using var glowPaint = new SKPaint
        {
            StrokeWidth = 8f,
            Color = new SKColor(80, 160, 255, (byte)(_trailAlpha / 3)),
            IsAntialias = true,
            StrokeCap = _trailPaint.StrokeCap,
            StrokeJoin = _trailPaint.StrokeJoin,
            Style = _trailPaint.Style
        };
        canvas.DrawPath(path, glowPaint);

        _trailAlpha = (byte)Math.Max(0, _trailAlpha - 3);
    }

    // ─── Constellations ──────────────────────────────────────────

    private void DrawConstellations(SKCanvas canvas, SKSizeI size, SanctuaryConstellations constellations, double elapsed)
    {
        using var starPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var linePaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f, IsAntialias = true };

        foreach (var constellation in constellations.Stars)
        {
            var hash = constellation.Name.GetHashCode();
            var rng = new Random(hash);

            var baseX = (float)(Math.Abs(hash % size.Width));
            var baseY = (float)(Math.Abs((hash * 7) % (int)(size.Height * 0.5f)));

            var elementColor = constellation.Element switch
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

            var starPositions = new List<SKPoint>();
            int starCount = 3 + (hash % 5);

            for (int i = 0; i < starCount; i++)
            {
                var sx = baseX + (float)(rng.NextDouble() * 60 - 30);
                var sy = baseY + (float)(rng.NextDouble() * 40 - 20);
                starPositions.Add(new SKPoint(sx, sy));
            }

            // Draw connecting lines
            linePaint.Color = new SKColor(elementColor.Red, elementColor.Green, elementColor.Blue, 40);
            for (int i = 0; i < starPositions.Count - 1; i++)
                canvas.DrawLine(starPositions[i], starPositions[i + 1], linePaint);

            // Draw stars with twinkling
            for (int i = 0; i < starPositions.Count; i++)
            {
                var twinkle = MathF.Sin((float)elapsed * 3f + i * 1.7f) * 0.3f + 0.7f;
                var alpha = (byte)(120 + 135 * twinkle);
                var radius = 1.5f + twinkle;

                // Glow
                starPaint.Color = new SKColor(elementColor.Red, elementColor.Green, elementColor.Blue, (byte)(alpha / 3));
                canvas.DrawCircle(starPositions[i], radius * 3f, starPaint);

                // Core
                starPaint.Color = new SKColor(255, 255, 255, alpha);
                canvas.DrawCircle(starPositions[i], radius, starPaint);
            }
        }
    }

    // ─── Weather Effects ─────────────────────────────────────────

    private void DrawWeatherEffects(SKCanvas canvas, SKSizeI size, WeatherState weather, double elapsed)
    {
        using var paint = new SKPaint { IsAntialias = true };

        switch (weather.CurrentWeather)
        {
            case WeatherType.ManaRain:
                DrawRain(canvas, size, weather.Intensity, elapsed, paint);
                break;
            case WeatherType.Starstorm:
                DrawSnow(canvas, size, weather.Intensity, elapsed, paint);
                break;
            case WeatherType.Fog:
                DrawFog(canvas, size, weather.Intensity, paint);
                break;
            case WeatherType.Calm:
                DrawWind(canvas, size, weather.Intensity, elapsed, paint);
                break;
            case WeatherType.VoidBreach:
                DrawStorm(canvas, size, weather.Intensity, elapsed, paint);
                break;
            case WeatherType.Aurora:
                DrawAurora(canvas, size, weather.Intensity, elapsed, paint);
                break;
        }
    }

    private static void DrawRain(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.5f;

        var rng = new Random(42);
        int dropCount = (int)(60 * intensity);

        for (int i = 0; i < dropCount; i++)
        {
            var x = rng.Next(0, size.Width);
            var speed = 300f + (float)rng.NextDouble() * 200f;
            var y = (float)((elapsed * speed + rng.Next(0, size.Height)) % (size.Height + 40)) - 20;

            var alpha = (byte)(40 + 60 * intensity);
            paint.Color = new SKColor(120, 160, 255, alpha);

            canvas.DrawLine(x, y, x - 2, y + 12 + 8 * intensity, paint);
        }
    }

    private static void DrawSnow(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;

        var rng = new Random(99);
        int flakeCount = (int)(40 * intensity);

        for (int i = 0; i < flakeCount; i++)
        {
            var x = rng.Next(0, size.Width);
            var drift = MathF.Sin((float)elapsed * 0.5f + i * 0.3f) * 20f;
            var speed = 30f + (float)rng.NextDouble() * 40f;
            var y = (float)((elapsed * speed + rng.Next(0, size.Height)) % (size.Height + 20)) - 10;

            var alpha = (byte)(80 + 80 * intensity);
            paint.Color = new SKColor(255, 255, 255, alpha);

            canvas.DrawCircle(x + drift, y, 1.5f + (float)rng.NextDouble() * 1.5f, paint);
        }
    }

    private static void DrawFog(SKCanvas canvas, SKSizeI size, float intensity, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;

        var alpha = (byte)(20 + 60 * intensity);
        paint.Color = new SKColor(220, 220, 230, alpha);
        canvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), paint);
    }

    private static void DrawWind(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1f;

        var rng = new Random(77);
        int streakCount = (int)(20 * intensity);

        for (int i = 0; i < streakCount; i++)
        {
            var x = (float)((elapsed * 150 + rng.Next(0, size.Width)) % (size.Width + 100)) - 50;
            var y = rng.Next(0, size.Height);
            var len = 30f + (float)rng.NextDouble() * 50f;

            var alpha = (byte)(20 + 40 * intensity);
            paint.Color = new SKColor(200, 210, 220, alpha);

            canvas.DrawLine(x, y, x + len, y - 2, paint);
        }
    }

    private static void DrawStorm(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        // Heavy rain
        DrawRain(canvas, size, intensity * 1.5f, elapsed, paint);

        // Lightning flash
        var flashCycle = elapsed % 4.0;
        if (flashCycle < 0.15 && intensity > 0.3f)
        {
            paint.Style = SKPaintStyle.Fill;
            var flashAlpha = (byte)(60 + 40 * intensity);
            paint.Color = new SKColor(255, 255, 240, flashAlpha);
            canvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), paint);
        }
    }

    private static void DrawHeatwave(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        // Orange tint overlay
        paint.Style = SKPaintStyle.Fill;
        paint.Color = new SKColor(255, 140, 40, (byte)(15 + 20 * intensity));
        canvas.DrawRect(new SKRect(0, 0, size.Width, size.Height), paint);

        // Wavy distortion lines
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1f;

        using var path = new SKPath();
        int lineCount = (int)(5 + 8 * intensity);

        for (int i = 0; i < lineCount; i++)
        {
            var baseY = size.Height * 0.3f + i * (size.Height * 0.06f);
            path.Reset();
            path.MoveTo(0, baseY);

            for (float x = 0; x <= size.Width; x += 8f)
            {
                var wave = MathF.Sin(x * 0.02f + (float)elapsed * 2f + i * 0.5f) * (4f + 4f * intensity);
                path.LineTo(x, baseY + wave);
            }

            var alpha = (byte)(15 + 20 * intensity);
            paint.Color = new SKColor(255, 180, 80, alpha);
            canvas.DrawPath(path, paint);
        }
    }

    private static void DrawAurora(SKCanvas canvas, SKSizeI size, float intensity, double elapsed, SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;

        var bandCount = 3 + (int)(3 * intensity);
        var colors = new SKColor[]
        {
            new SKColor(80, 255, 160),
            new SKColor(100, 200, 255),
            new SKColor(180, 100, 255),
            new SKColor(100, 255, 200)
        };

        for (int i = 0; i < bandCount; i++)
        {
            var color = colors[i % colors.Length];
            var baseY = size.Height * 0.05f + i * (size.Height * 0.04f);

            using var path = new SKPath();
            path.MoveTo(0, baseY);

            for (float x = 0; x <= size.Width; x += 6f)
            {
                var wave = MathF.Sin(x * 0.008f + (float)elapsed * 0.4f + i * 1.2f) * (15f + 10f * intensity);
                path.LineTo(x, baseY + wave);
            }

            for (float x = size.Width; x >= 0; x -= 6f)
            {
                var wave = MathF.Sin(x * 0.008f + (float)elapsed * 0.4f + i * 1.2f) * (15f + 10f * intensity);
                path.LineTo(x, baseY + wave + 8f + 6f * intensity);
            }

            path.Close();

            var alpha = (byte)(30 + 50 * intensity);
            paint.Color = new SKColor(color.Red, color.Green, color.Blue, alpha);
            canvas.DrawPath(path, paint);
        }
    }

    // ─── Utilities ───────────────────────────────────────────────

    private static SKColor LerpColor(SKColor a, SKColor b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t));
    }

    public (float X, float Y) GridToPixel(int col, int row) =>
        (col * _cellWidth + _cellWidth / 2f, row * _cellHeight + _cellHeight / 2f);

    public (int Col, int Row) PixelToGrid(float x, float y) =>
        ((int)(x / _cellWidth), (int)(y / _cellHeight));
}
