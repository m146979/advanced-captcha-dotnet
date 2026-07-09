using CaptchaBehavioral.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace CaptchaBehavioral.Services;

public class ModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly ILogger<ModelTrainer> _logger;

    public ModelTrainer(ILogger<ModelTrainer> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
    }

    public void TrainAndSave(string trainingDataPath, string modelOutputPath)
    {
        _logger.LogInformation("Loading training data from {Path}", trainingDataPath);
        var data = _mlContext.Data.LoadFromTextFile<BehavioralFeatures>(
            trainingDataPath, separatorChar: ',', hasHeader: true);

        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        var featureColumns = typeof(BehavioralFeatures).GetProperties()
            .Where(p => p.Name != nameof(BehavioralFeatures.IsHuman))
            .Select(p => p.Name).ToArray();

        var pipeline = _mlContext.Transforms
            .Concatenate("Features", featureColumns)
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 50,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.1));

        _logger.LogInformation("Training RandomForest/FastTree model...");
        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions);

        _logger.LogInformation("Model metrics: Accuracy={Acc:F3}, AUC={Auc:F3}, F1={F1:F3}",
            metrics.Accuracy, metrics.AreaUnderRocCurve, metrics.F1Score);

        _mlContext.Model.Save(model, data.Schema, modelOutputPath);
        _logger.LogInformation("Model saved to {Path}", modelOutputPath);
    }

    public static void GenerateSyntheticTrainingData(string outputPath, int samples = 5000)
    {
        var rng = new Random(42);
        var lines = new List<string> {
            "MouseEventCount,AvgMouseSpeed,MaxMouseSpeed,MouseTrajectorySmoothnessScore,MouseJitterScore," +
            "PerfectLinearMovements,TeleportationEvents,TimeOnPageMs,KeystrokeCount,AvgKeystrokeInterval," +
            "KeystrokeIntervalVariance,ScrollEventCount,AvgScrollVelocity,ScrollVelocityVariance," +
            "TouchEventCount,FocusBlurCount,AccelerationVariance,DirectionChangeCount,PauseCount,AvgPauseDuration,IsHuman"
        };

        for (int i = 0; i < samples; i++)
        {
            bool isHuman = i % 2 == 0;
            string row;
            if (isHuman)
            {
                row = $"{rng.Next(20, 200)},{rng.Next(100, 500)},{rng.Next(500, 2000)}," +
                      $"{rng.NextDouble() * 0.4 + 0.6:F3},{rng.NextDouble() * 5 + 1:F3}," +
                      $"{rng.Next(0, 2)},0,{rng.Next(3000, 60000)}," +
                      $"{rng.Next(5, 50)},{rng.Next(80, 300)},{rng.Next(50, 500)}," +
                      $"{rng.Next(1, 20)},{rng.NextDouble() * 100 + 10:F3},{rng.NextDouble() * 50 + 5:F3}," +
                      $"0,{rng.Next(0, 5)},{rng.NextDouble() * 200 + 10:F3},{rng.Next(3, 30)}," +
                      $"{rng.Next(1, 10)},{rng.Next(100, 1000)},True";
            }
            else
            {
                row = $"{rng.Next(0, 10)},{rng.Next(800, 5000)},{rng.Next(5000, 20000)}," +
                      $"{rng.NextDouble() * 0.1:F3},{rng.NextDouble() * 0.2:F3}," +
                      $"{rng.Next(5, 20)},{rng.Next(1, 5)},{rng.Next(100, 2000)}," +
                      $"0,0,0," +
                      $"0,0,0," +
                      $"0,0,{rng.NextDouble() * 2:F3},{rng.Next(0, 1)}," +
                      $"0,0,False";
            }
            lines.Add(row);
        }

        File.WriteAllLines(outputPath, lines);
    }
}
