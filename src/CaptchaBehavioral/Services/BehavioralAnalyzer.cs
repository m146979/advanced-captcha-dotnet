using CaptchaCore.Configuration;
using CaptchaCore.Models;
using CaptchaBehavioral.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace CaptchaBehavioral.Services;

public interface IBehavioralAnalyzer
{
    double Analyze(BehavioralData data);
    Task<bool> LoadModelAsync(string modelPath);
}

public class BehavioralAnalyzer : IBehavioralAnalyzer
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<BehavioralFeatures, BehavioralPrediction>? _predictionEngine;
    private readonly CaptchaOptions _options;
    private readonly ILogger<BehavioralAnalyzer> _logger;

    public BehavioralAnalyzer(IOptions<CaptchaOptions> options, ILogger<BehavioralAnalyzer> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> LoadModelAsync(string modelPath)
    {
        try
        {
            await Task.Run(() =>
            {
                _model = _mlContext.Model.Load(modelPath, out _);
                _predictionEngine = _mlContext.Model
                    .CreatePredictionEngine<BehavioralFeatures, BehavioralPrediction>(_model);
            });
            _logger.LogInformation("Behavioral ML model loaded from {Path}", modelPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load ML model from {Path}, using heuristics", modelPath);
            return false;
        }
    }

    public double Analyze(BehavioralData data)
    {
        var features = ExtractFeatures(data);

        if (_predictionEngine != null)
        {
            var prediction = _predictionEngine.Predict(features);
            _logger.LogDebug("ML prediction: IsHuman={IsHuman}, Probability={Prob}",
                prediction.IsHuman, prediction.Probability);
            return prediction.Probability;
        }

        return HeuristicScore(features, data);
    }

    private BehavioralFeatures ExtractFeatures(BehavioralData data)
    {
        var mouseEvents = data.MouseEvents;
        var features = new BehavioralFeatures
        {
            MouseEventCount = mouseEvents.Count,
            TimeOnPageMs = data.TimeOnPage,
            KeystrokeCount = data.KeyEvents.Count,
            ScrollEventCount = data.ScrollEvents.Count,
            TouchEventCount = data.TouchEventCount,
            FocusBlurCount = data.FocusBlurCount
        };

        if (mouseEvents.Count > 1)
        {
            var speeds = new List<double>();
            var accelerations = new List<double>();
            int teleports = 0, linearSeqs = 0, pauses = 0, dirChanges = 0;
            double prevSpeed = 0;

            for (int i = 1; i < mouseEvents.Count; i++)
            {
                var dx = mouseEvents[i].X - mouseEvents[i - 1].X;
                var dy = mouseEvents[i].Y - mouseEvents[i - 1].Y;
                var dt = (mouseEvents[i].Timestamp - mouseEvents[i - 1].Timestamp) / 1000.0;
                if (dt <= 0) dt = 0.001;

                var dist = Math.Sqrt(dx * dx + dy * dy);
                var speed = dist / dt;
                speeds.Add(speed);

                if (dist > 500) teleports++;
                if (dt > 1.0) pauses++;

                var accel = (speed - prevSpeed) / dt;
                accelerations.Add(Math.Abs(accel));
                prevSpeed = speed;

                if (i > 1)
                {
                    var prevDx = mouseEvents[i - 1].X - mouseEvents[i - 2].X;
                    var prevDy = mouseEvents[i - 1].Y - mouseEvents[i - 2].Y;
                    var dot = dx * prevDx + dy * prevDy;
                    var mag = dist * Math.Sqrt(prevDx * prevDx + prevDy * prevDy);
                    if (mag > 0 && dot / mag < -0.5) dirChanges++;
                }
            }

            features.AvgMouseSpeed = (float)(speeds.Count > 0 ? speeds.Average() : 0);
            features.MaxMouseSpeed = (float)(speeds.Count > 0 ? speeds.Max() : 0);
            features.TeleportationEvents = teleports;
            features.PauseCount = pauses;
            features.DirectionChangeCount = dirChanges;
            features.AccelerationVariance = (float)Variance(accelerations);

            // Jitter: average perpendicular deviation from straight line between start/end
            features.MouseJitterScore = (float)ComputeJitter(mouseEvents);
            features.MouseTrajectorySmoothnessScore = (float)ComputeSmoothness(mouseEvents);
            features.PerfectLinearMovements = DetectPerfectLinear(mouseEvents);
        }

        if (data.KeyEvents.Count > 1)
        {
            var intervals = new List<double>();
            for (int i = 1; i < data.KeyEvents.Count; i++)
                intervals.Add(data.KeyEvents[i].Timestamp - data.KeyEvents[i - 1].Timestamp);
            features.AvgKeystrokeInterval = (float)intervals.Average();
            features.KeystrokeIntervalVariance = (float)Variance(intervals);
        }

        if (data.ScrollEvents.Count > 0)
        {
            var velocities = data.ScrollEvents.Select(s => (double)s.Velocity).ToList();
            features.AvgScrollVelocity = (float)velocities.Average();
            features.ScrollVelocityVariance = (float)Variance(velocities);
        }

        return features;
    }

    private double HeuristicScore(BehavioralFeatures f, BehavioralData data)
    {
        double score = 0.5;

        if (f.TimeOnPageMs < _options.BehavioralAnalysis.MinInteractionTime) score -= 0.3;
        if (f.MouseEventCount < _options.BehavioralAnalysis.RequiredMouseMovements) score -= 0.2;
        if (f.TeleportationEvents > 0) score -= 0.25;
        if (f.PerfectLinearMovements > 3) score -= 0.2;
        if (f.KeystrokeIntervalVariance < 1 && f.KeystrokeCount > 3) score -= 0.15;
        if (f.MouseJitterScore > 0.1) score += 0.15;
        if (f.DirectionChangeCount > 2) score += 0.1;
        if (f.FocusBlurCount > 0) score += 0.05;
        if (f.ScrollEventCount > 0) score += 0.05;

        return Math.Clamp(score, 0.0, 1.0);
    }

    private static double ComputeJitter(List<MouseEvent> events)
    {
        if (events.Count < 3) return 0;
        var start = events.First();
        var end = events.Last();
        double dx = end.X - start.X, dy = end.Y - start.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1) return 0;

        double totalDev = 0;
        foreach (var e in events)
        {
            double t = ((e.X - start.X) * dx + (e.Y - start.Y) * dy) / (len * len);
            double projX = start.X + t * dx, projY = start.Y + t * dy;
            totalDev += Math.Sqrt(Math.Pow(e.X - projX, 2) + Math.Pow(e.Y - projY, 2));
        }
        return totalDev / events.Count;
    }

    private static double ComputeSmoothness(List<MouseEvent> events)
    {
        if (events.Count < 3) return 1;
        double totalAngleChange = 0;
        for (int i = 1; i < events.Count - 1; i++)
        {
            var a = events[i - 1]; var b = events[i]; var c = events[i + 1];
            double v1x = b.X - a.X, v1y = b.Y - a.Y;
            double v2x = c.X - b.X, v2y = c.Y - b.Y;
            double dot = v1x * v2x + v1y * v2y;
            double m1 = Math.Sqrt(v1x * v1x + v1y * v1y);
            double m2 = Math.Sqrt(v2x * v2x + v2y * v2y);
            if (m1 > 0 && m2 > 0)
                totalAngleChange += Math.Acos(Math.Clamp(dot / (m1 * m2), -1, 1));
        }
        return 1.0 - Math.Min(totalAngleChange / (events.Count * Math.PI), 1.0);
    }

    private static int DetectPerfectLinear(List<MouseEvent> events)
    {
        int count = 0;
        for (int i = 0; i + 4 < events.Count; i++)
        {
            var seg = events.Skip(i).Take(5).ToList();
            var jitter = ComputeJitter(seg);
            if (jitter < 0.5) count++;
        }
        return count;
    }

    private static double Variance(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        var avg = list.Average();
        return list.Select(v => Math.Pow(v - avg, 2)).Average();
    }
}
