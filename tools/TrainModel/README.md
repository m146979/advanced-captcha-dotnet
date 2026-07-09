# ModelTrainer Tool

Console app to generate synthetic training data and train the ML.NET behavioral model.

## Commands

```bash
# From repo root:

# Generate 10,000 synthetic rows + train (default)
dotnet run --project tools/TrainModel -- all

# Generate only (20,000 samples)
dotnet run --project tools/TrainModel -- generate --samples 20000

# Train only (uses existing training_data.csv)
dotnet run --project tools/TrainModel -- train
```

## Output

- `training_data.csv` — generated next to the tool binary (gitignored)
- `src/CaptchaApi/models/behavioral_model.zip` — compiled ML.NET model loaded by the API at startup

## What the synthetic data looks like

| Feature | Human profile | Bot profile |
|---------|--------------|-------------|
| MouseEventCount | 20–200 events | 0–10 events |
| AvgMouseSpeed | 100–500 px/s | 800–5000 px/s |
| TeleportationEvents | 0 | 1–5 |
| MouseJitterScore | 1–6 (natural) | 0–0.2 (perfect) |
| PerfectLinearMovements | 0–1 | 5–20 |
| TimeOnPageMs | 3,000–60,000 ms | 100–2,000 ms |
| KeystrokeIntervalVariance | 50–500 (natural) | 0 (robotic) |
| AccelerationVariance | 10–210 (natural) | 0–2 (constant) |

## Using real production data

1. In `BehavioralAnalyzer`, log feature vectors to a CSV for borderline sessions (score 0.5–0.75)
2. Manually label them as `True` (human) or `False` (bot)
3. Append to `training_data.csv` and retrain
4. See `docs/ml-model-training.md` for the full retraining workflow
