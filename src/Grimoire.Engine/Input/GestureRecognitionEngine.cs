using System.Numerics;
using Grimoire.Core.Enums;

namespace Grimoire.Engine.Input;

/// <summary>
/// Lightweight gesture recognition engine with Catmull-Rom interpolation.
/// Captures a sequence of 2D points from mouse/touchpad input and
/// classifies the drawing into one of the predefined SpellGesture shapes.
///
/// Recognition pipeline:
///   1. Interpolate raw points via Catmull-Rom splines for smooth curves.
///   2. Resample the stroke to N evenly-spaced points.
///   3. Normalise to a bounding-box-centered, unit-scaled coordinate space.
///   4. Compare against template shapes using cosine similarity + geometric heuristics.
/// </summary>
public sealed class GestureRecognitionEngine
{
    private const int ResampleCount = 64;
    private const int MinPointsForRecognition = 8;
    private const float ClosedShapeThreshold = 0.35f;
    private const float AcceptanceThreshold = 0.72f;

    /// <summary>Number of interpolated points to insert between each raw point pair.</summary>
    public int InterpolationSteps { get; set; } = 4;

    private readonly List<Vector2> _rawPoints = [];
    private List<float> _lastStrokeDeviations = [];
    private float _lastStrokeSpeed;
    private float _lastStrokeDuration;
    private DateTimeOffset _strokeStartTime;

    public void BeginStroke()
    {
        _rawPoints.Clear();
        _strokeStartTime = DateTimeOffset.UtcNow;
    }

    public void AddPoint(Vector2 point) => _rawPoints.Add(point);

    public SpellGesture EndStroke()
    {
        _lastStrokeDuration = (float)(DateTimeOffset.UtcNow - _strokeStartTime).TotalSeconds;

        if (_rawPoints.Count < MinPointsForRecognition)
        {
            _lastStrokeDeviations = [];
            _lastStrokeSpeed = 0;
            _rawPoints.Clear();
            return SpellGesture.Unknown;
        }

        // Step 1: Interpolate for smooth curves
        var interpolated = InterpolateCatmullRom(_rawPoints, InterpolationSteps);

        // Step 2: Resample to uniform point count
        var resampled = Resample(interpolated, ResampleCount);

        // Step 3: Normalise
        var normalised = Normalise(resampled);

        // Step 4: Classify
        var gesture = Classify(normalised);

        // Record quality metrics
        var totalLen = PathLength(_rawPoints);
        _lastStrokeSpeed = _lastStrokeDuration > 0 ? totalLen / _lastStrokeDuration : 0;
        _lastStrokeDeviations = _rawPoints
            .Select((p, i) => i == 0 ? 0f : Vector2.Distance(p, _rawPoints[i - 1]))
            .ToList();

        _rawPoints.Clear();
        return gesture;
    }

    public IReadOnlyList<float> GetLastStrokeDeviations() => _lastStrokeDeviations.AsReadOnly();
    public float GetLastStrokeSpeed() => _lastStrokeSpeed;
    public float GetLastStrokeDuration() => _lastStrokeDuration;

    public IReadOnlyList<Vector2> GetCurrentStroke() => _rawPoints.AsReadOnly();

    // ─── Catmull-Rom Interpolation ───────────────────────────────

    /// <summary>
    /// Inserts smooth interpolated points between each pair of raw points
    /// using the Catmull-Rom spline formula. This reduces jitter from
    /// uneven mouse sampling rates and produces cleaner curves for recognition.
    /// </summary>
    private static List<Vector2> InterpolateCatmullRom(List<Vector2> points, int steps)
    {
        if (points.Count < 3 || steps < 1)
            return [.. points];

        var result = new List<Vector2> { points[0] };

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[Math.Min(points.Count - 1, i + 1)];
            var p3 = points[Math.Min(points.Count - 1, i + 2)];

            for (int t = 1; t <= steps; t++)
            {
                var frac = t / (float)(steps + 1);
                var interpolated = CatmullRom(p0, p1, p2, p3, frac);
                result.Add(interpolated);
            }

            result.Add(p2);
        }

        return result;
    }

    /// <summary>
    /// Standard Catmull-Rom spline evaluation.
    /// q(t) = 0.5 * ((2*P1) + (-P0 + P2)*t + (2*P0 - 5*P1 + 4*P2 - P3)*t^2 + (-P0 + 3*P1 - 3*P2 + P3)*t^3)
    /// </summary>
    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    // ─── Resampling ──────────────────────────────────────────────

    private static List<Vector2> Resample(List<Vector2> points, int n)
    {
        var total = PathLength(points);
        var interval = total / (n - 1);
        var result = new List<Vector2> { points[0] };
        var accumulated = 0f;
        var pts = new List<Vector2>(points);

        for (int i = 1; i < pts.Count; i++)
        {
            var d = Vector2.Distance(pts[i - 1], pts[i]);
            if (accumulated + d >= interval)
            {
                var ratio = (interval - accumulated) / d;
                var newX = pts[i - 1].X + ratio * (pts[i].X - pts[i - 1].X);
                var newY = pts[i - 1].Y + ratio * (pts[i].Y - pts[i - 1].Y);
                var interpolated = new Vector2(newX, newY);
                result.Add(interpolated);
                pts.Insert(i, interpolated);
                accumulated = 0;
            }
            else
            {
                accumulated += d;
            }
        }

        while (result.Count < n)
            result.Add(pts[^1]);

        return result;
    }

    // ─── Normalisation ───────────────────────────────────────────

    private static List<Vector2> Normalise(List<Vector2> points)
    {
        var centroid = new Vector2(
            points.Average(p => p.X),
            points.Average(p => p.Y));

        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxX = points.Max(p => p.X);
        var maxY = points.Max(p => p.Y);
        var size = new Vector2(maxX - minX, maxY - minY);
        var largest = Math.Max(size.X, size.Y);

        if (largest < 0.0001f)
            return points;

        return points
            .Select(p => (p - centroid) / largest)
            .ToList();
    }

    // ─── Classification ──────────────────────────────────────────

    private static SpellGesture Classify(List<Vector2> points)
    {
        var scores = new Dictionary<SpellGesture, float>
        {
            [SpellGesture.Circle] = ScoreCircle(points),
            [SpellGesture.Triangle] = ScoreTriangle(points),
            [SpellGesture.Line] = ScoreLine(points),
            [SpellGesture.Zigzag] = ScoreZigzag(points),
            [SpellGesture.Spiral] = ScoreSpiral(points)
        };

        var best = scores.MaxBy(kv => kv.Value);
        return best.Value >= AcceptanceThreshold ? best.Key : SpellGesture.Unknown;
    }

    private static float ScoreCircle(List<Vector2> points)
    {
        var centroid = new Vector2(points.Average(p => p.X), points.Average(p => p.Y));
        var distances = points.Select(p => Vector2.Distance(p, centroid)).ToList();
        var meanDist = distances.Average();
        if (meanDist < 0.0001f) return 0;

        var variance = distances.Average(d => (d - meanDist) * (d - meanDist));
        var cv = MathF.Sqrt(variance) / meanDist;
        var closedness = 1f - MathF.Min(1f, Vector2.Distance(points[0], points[^1]) / ClosedShapeThreshold);
        var radiusScore = MathF.Max(0f, 1f - cv * 3f);

        return radiusScore * 0.7f + closedness * 0.3f;
    }

    private static float ScoreTriangle(List<Vector2> points)
    {
        var corners = DetectCorners(points);
        var closedness = 1f - MathF.Min(1f, Vector2.Distance(points[0], points[^1]) / ClosedShapeThreshold);
        var cornerScore = corners == 3 ? 1f : corners == 2 ? 0.6f : 0.2f;

        return cornerScore * 0.7f + closedness * 0.3f;
    }

    private static float ScoreLine(List<Vector2> points)
    {
        var startEnd = Vector2.Distance(points[0], points[^1]);
        var totalLen = PathLength(points);
        if (totalLen < 0.0001f) return 0;

        var straightness = startEnd / totalLen;
        var separation = MathF.Min(1f, startEnd / 0.5f);

        return straightness * 0.8f + separation * 0.2f;
    }

    private static float ScoreZigzag(List<Vector2> points)
    {
        var signs = GetAngularSigns(points);
        if (signs.Count < 3) return 0.1f;

        int changes = 0;
        for (int i = 1; i < signs.Count; i++)
        {
            if (signs[i] != signs[i - 1]) changes++;
        }

        var changeRatio = (float)changes / (signs.Count - 1);
        var openness = 1f - MathF.Min(1f, Vector2.Distance(points[0], points[^1]) / ClosedShapeThreshold);

        return changeRatio * 0.6f + openness * 0.4f;
    }

    private static float ScoreSpiral(List<Vector2> points)
    {
        var centroid = new Vector2(points.Average(p => p.X), points.Average(p => p.Y));
        var distances = points.Select(p => Vector2.Distance(p, centroid)).ToList();

        int violations = 0;
        for (int i = 2; i < distances.Count; i++)
        {
            var trend1 = distances[i - 1] - distances[i - 2];
            var trend2 = distances[i] - distances[i - 1];

            if (trend1 * trend2 < 0 && MathF.Abs(trend2) > 0.005f)
                violations++;
        }

        var monotonicity = 1f - (float)violations / (distances.Count - 2);
        var openness = 1f - MathF.Min(1f, Vector2.Distance(points[0], points[^1]) / ClosedShapeThreshold);

        return monotonicity * 0.6f + openness * 0.4f;
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private static int DetectCorners(List<Vector2> points)
    {
        var angles = GetAngles(points);
        var threshold = MathF.PI * 0.6f;
        return angles.Count(a => a < threshold);
    }

    private static List<float> GetAngles(List<Vector2> points)
    {
        var angles = new List<float>();
        for (int i = 1; i < points.Count - 1; i++)
        {
            var v1 = points[i - 1] - points[i];
            var v2 = points[i + 1] - points[i];
            var dot = Vector2.Dot(v1, v2) / (v1.Length() * v2.Length() + 0.0001f);
            dot = Math.Clamp(dot, -1f, 1f);
            angles.Add(MathF.Acos(dot));
        }
        return angles;
    }

    private static List<int> GetAngularSigns(List<Vector2> points)
    {
        var signs = new List<int>();
        for (int i = 2; i < points.Count; i++)
        {
            var cross = (points[i - 1].X - points[i - 2].X) * (points[i].Y - points[i - 1].Y)
                       - (points[i - 1].Y - points[i - 2].Y) * (points[i].X - points[i - 1].X);
            signs.Add(cross >= 0 ? 1 : -1);
        }
        return signs;
    }

    private static float PathLength(List<Vector2> points)
    {
        float total = 0;
        for (int i = 1; i < points.Count; i++)
            total += Vector2.Distance(points[i - 1], points[i]);
        return total;
    }
}
