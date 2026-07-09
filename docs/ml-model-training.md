# ML Model Training Guide

## Overview

The behavioral analysis model uses ML.NET FastTree (Gradient Boosted Decision Trees).
Training is done offline; the compiled model (`.zip`) is loaded at API startup.

## Step 1: Generate or collect training data

### Option A: Synthetic data (quick start)
```csharp
// In a console app or test harness:
ModelTrainer.GenerateSyntheticTrainingData("training_data.csv", samples: 10000);
```

### Option B: Real labeled data
Log behavioral feature vectors from production with human/bot labels:
```csharp
// In your verify endpoint, after scoring:
logger.LogInformation("TRAINING_DATA|{Features}|{Label}",
    JsonSerializer.Serialize(features), isHuman ? "True" : "False");
```
Then parse logs into CSV format matching `BehavioralFeatures` column order.

## Step 2: Train the model

```csharp
var trainer = new ModelTrainer(logger);
trainer.TrainAndSave("training_data.csv", "models/behavioral_model.zip");
```

Expected output:
```
Model metrics: Accuracy=0.923, AUC=0.971, F1=0.919
Model saved to models/behavioral_model.zip
```

## Step 3: Deploy

Place `behavioral_model.zip` in the `models/` folder next to the API binary.
The API loads it automatically at startup:
```csharp
var modelPath = Path.Combine("models", "behavioral_model.zip");
if (File.Exists(modelPath))
    await analyzer.LoadModelAsync(modelPath);
```

## Step 4: Retraining schedule

- Retrain monthly or when false positive rate exceeds 5%
- Add newly captured bot patterns as negative examples
- Keep a holdout test set (20%) for unbiased evaluation
- Version models: `behavioral_model_v2_20260101.zip`

## Features Reference

| # | Feature | Description |
|---|---------|-------------|
| 0 | `MouseEventCount` | Total mouse move events |
| 1 | `AvgMouseSpeed` | Average pixels/ms |
| 2 | `MaxMouseSpeed` | Peak mouse speed |
| 3 | `MouseTrajectorySmoothnessScore` | 0-1, higher = smoother |
| 4 | `MouseJitterScore` | Perpendicular deviation from straight line |
| 5 | `PerfectLinearMovements` | Count of suspiciously straight segments |
| 6 | `TeleportationEvents` | Jumps > 500px instantly |
| 7 | `TimeOnPageMs` | Total time before submission |
| 8-10 | Keystroke metrics | Count, avg interval, variance |
| 11-13 | Scroll metrics | Count, avg velocity, variance |
| 14 | `TouchEventCount` | Mobile touch events |
| 15 | `FocusBlurCount` | Tab switches |
| 16-19 | Advanced motion | Acceleration variance, direction changes, pauses |
