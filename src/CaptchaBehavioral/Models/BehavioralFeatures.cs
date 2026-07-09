using Microsoft.ML.Data;

namespace CaptchaBehavioral.Models;

public class BehavioralFeatures
{
    [LoadColumn(0)] public float MouseEventCount { get; set; }
    [LoadColumn(1)] public float AvgMouseSpeed { get; set; }
    [LoadColumn(2)] public float MaxMouseSpeed { get; set; }
    [LoadColumn(3)] public float MouseTrajectorySmoothnessScore { get; set; }
    [LoadColumn(4)] public float MouseJitterScore { get; set; }
    [LoadColumn(5)] public float PerfectLinearMovements { get; set; }
    [LoadColumn(6)] public float TeleportationEvents { get; set; }
    [LoadColumn(7)] public float TimeOnPageMs { get; set; }
    [LoadColumn(8)] public float KeystrokeCount { get; set; }
    [LoadColumn(9)] public float AvgKeystrokeInterval { get; set; }
    [LoadColumn(10)] public float KeystrokeIntervalVariance { get; set; }
    [LoadColumn(11)] public float ScrollEventCount { get; set; }
    [LoadColumn(12)] public float AvgScrollVelocity { get; set; }
    [LoadColumn(13)] public float ScrollVelocityVariance { get; set; }
    [LoadColumn(14)] public float TouchEventCount { get; set; }
    [LoadColumn(15)] public float FocusBlurCount { get; set; }
    [LoadColumn(16)] public float AccelerationVariance { get; set; }
    [LoadColumn(17)] public float DirectionChangeCount { get; set; }
    [LoadColumn(18)] public float PauseCount { get; set; }
    [LoadColumn(19)] public float AvgPauseDuration { get; set; }

    [LoadColumn(20), ColumnName("Label")]
    public bool IsHuman { get; set; }
}

public class BehavioralPrediction
{
    [ColumnName("PredictedLabel")] public bool IsHuman { get; set; }
    [ColumnName("Probability")] public float Probability { get; set; }
    [ColumnName("Score")] public float Score { get; set; }
}
