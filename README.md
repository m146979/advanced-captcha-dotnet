# Advanced Self-Hosted CAPTCHA System (.NET)

A sophisticated, multi-layered self-hosted CAPTCHA system built with ASP.NET Core 8 that effectively deters AI bots while maintaining excellent user experience.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    CAPTCHA System                        │
├─────────────┬──────────────┬────────────┬───────────────┤
│  Layer 1    │   Layer 2    │  Layer 3   │   Layer 4     │
│  Proof of   │  Behavioral  │Interactive │Fingerprinting │
│   Work      │  Analysis    │ Challenges │  & Detection  │
│  (Always)   │  (Always)    │(Suspicious)│  (Continuous) │
└─────────────┴──────────────┴────────────┴───────────────┘
```

## Features

- **Layer 1 – Proof of Work**: SHA-256 based PoW with dynamic difficulty (3–6 leading zeros), IP reputation tracking via Redis
- **Layer 2 – Behavioral Analysis**: Mouse, keystroke, scroll, touch event collection + ML.NET RandomForest classification
- **Layer 3 – Interactive Challenges**: Physics puzzles, SkiaSharp image challenges, temporal click challenges, logical reasoning
- **Layer 4 – Fingerprinting**: Headless browser detection, honeypot fields, TLS JA3/JA4 fingerprinting, request pattern analysis

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend   | ASP.NET Core 8, C# |
| Image Gen | SkiaSharp 2.88+ |
| ML Model  | ML.NET 3.0+ (RandomForest) |
| Caching   | StackExchange.Redis |
| Crypto    | System.Security.Cryptography |
| Frontend  | Vanilla JS (ES6+), Web Crypto API |

## Project Structure

```
advanced-captcha-dotnet/
├── src/
│   ├── CaptchaCore/              # Core library
│   ├── CaptchaApi/               # ASP.NET Core API
│   ├── CaptchaBehavioral/        # ML.NET behavioral analysis
│   ├── CaptchaChallenges/        # Interactive challenge types
│   └── CaptchaMiddleware/        # Fingerprinting middleware
├── client/
│   ├── captcha-sdk.js            # Main JS SDK
│   ├── pow-worker.js             # Web Worker for PoW
│   └── behavioral-collector.js   # Behavioral data collector
├── docs/
│   ├── integration-guide.md
│   ├── configuration-reference.md
│   ├── security-best-practices.md
│   └── ml-model-training.md
├── tests/
│   ├── CaptchaCore.Tests/
│   └── CaptchaApi.Tests/
└── docker-compose.yml
```

## Quick Start

### Prerequisites
- .NET 8 SDK
- Redis (or Docker)
- Node.js (for client build, optional)

### Run with Docker Compose

```bash
git clone https://github.com/m146979/advanced-captcha-dotnet
cd advanced-captcha-dotnet
docker-compose up -d
```

### Run Locally

```bash
# Start Redis
docker run -d -p 6379:6379 redis:7-alpine

# Run API
cd src/CaptchaApi
dotnet run
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/captcha/challenge` | Get PoW challenge |
| POST | `/api/captcha/verify` | Verify PoW solution |
| POST | `/api/captcha/interactive/get` | Get interactive challenge |
| POST | `/api/captcha/interactive/verify` | Verify interactive answer |
| GET  | `/api/captcha/image/{id}` | Get rendered challenge image |

## Configuration

See [docs/configuration-reference.md](docs/configuration-reference.md) for full options.

## Security Notes

- This system significantly raises the cost for attackers but is not 100% bot-proof
- Advanced AI (GPT-4 Vision, Claude) can solve image challenges — rely on PoW + behavioral layers as primary defense
- Regularly update challenge variations to prevent ML models from learning patterns
- Monitor false positive rates and adjust thresholds accordingly
- Consider GDPR implications when collecting behavioral data

## License

MIT
