# Configuration Reference

All settings live under the `Captcha` key in `appsettings.json`.

## Core Settings

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `DefaultDifficulty` | int | 4 | Leading zeros required by default |
| `MinDifficulty` | int | 3 | Minimum PoW difficulty (Easy) |
| `MaxDifficulty` | int | 6 | Maximum PoW difficulty (Very Hard) |
| `ChallengeExpiryMinutes` | int | 5 | PoW challenge TTL in Redis |
| `TokenExpiryMinutes` | int | 10 | Issued token lifetime |
| `HmacSecret` | string | *(required)* | 256-bit+ secret for HMAC signing |

## Rate Limiting

| Key | Default | Description |
|-----|---------|-------------|
| `RateLimit.MaxRequestsPerMinute` | 10 | Requests per IP per minute (sliding window) |
| `RateLimit.MaxFailuresBeforeBlock` | 5 | Failures before IP is blocked |
| `RateLimit.BlockDurationMinutes` | 30 | Block duration in minutes |

## Behavioral Analysis

| Key | Default | Description |
|-----|---------|-------------|
| `BehavioralAnalysis.MinInteractionTime` | 2000 | Minimum ms on page before submission |
| `BehavioralAnalysis.RequiredMouseMovements` | 5 | Minimum mouse events for human score |
| `BehavioralAnalysis.SuspicionThreshold` | 0.5 | Score below this triggers interactive challenge |
| `BehavioralAnalysis.HumanConfidenceThreshold` | 0.75 | Score above this passes without interactive |

## Secrets Management

Never put `HmacSecret` in committed config files. Use:

```bash
# Environment variable
export Captcha__HmacSecret=$(openssl rand -hex 32)

# Docker
environment:
  - Captcha__HmacSecret=${CAPTCHA_HMAC_SECRET}

# Azure Key Vault / AWS Secrets Manager / HashiCorp Vault for production
```

## Redis Connection

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpass,ssl=true"
  }
}
```
