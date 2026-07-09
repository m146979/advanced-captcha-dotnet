using CaptchaBehavioral.Services;
using Microsoft.Extensions.Logging;

/**
 * Usage:
 *   dotnet run --project tools/TrainModel -- generate          (create synthetic CSV)
 *   dotnet run --project tools/TrainModel -- train             (train on existing CSV)
 *   dotnet run --project tools/TrainModel -- all               (generate + train)
 *   dotnet run --project tools/TrainModel -- all --samples 20000
 */

var command = args.FirstOrDefault("all");
var samplesArg = args.SkipWhile(a => a != "--samples").Skip(1).FirstOrDefault();
int samples = samplesArg != null && int.TryParse(samplesArg, out var s) ? s : 10_000;

var dataPath = Path.GetFullPath("training_data.csv");
var modelsDir = Path.GetFullPath(Path.Combine("..", "..", "src", "CaptchaApi", "models"));
Directory.CreateDirectory(modelsDir);
var modelPath = Path.Combine(modelsDir, "behavioral_model.zip");

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<ModelTrainer>();

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n=== Advanced CAPTCHA - ML Model Trainer ===");
Console.ResetColor();

if (command is "generate" or "all")
{
    Console.WriteLine($"\n[1/2] Generating {samples:N0} synthetic training samples...");
    ModelTrainer.GenerateSyntheticTrainingData(dataPath, samples);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"      ✓ Saved to: {dataPath}");
    Console.ResetColor();
}

if (command is "train" or "all")
{
    if (!File.Exists(dataPath))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ERROR] Training data not found at: {dataPath}");
        Console.WriteLine("        Run with 'generate' or 'all' first.");
        Console.ResetColor();
        return 1;
    }

    Console.WriteLine($"\n[2/2] Training FastTree model on {dataPath}...");
    var trainer = new ModelTrainer(logger);
    trainer.TrainAndSave(dataPath, modelPath);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n      ✓ Model saved to: {modelPath}");
    Console.WriteLine("      ✓ Drop behavioral_model.zip into src/CaptchaApi/models/ if not already there.");
    Console.ResetColor();
}

Console.WriteLine("\nDone.\n");
return 0;
