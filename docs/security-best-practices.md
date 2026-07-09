# Security Best Practices

## HMAC Secret
- Minimum 256 bits (32 bytes) of cryptographic randomness
- Rotate every 90 days; invalidate all active tokens on rotation
- Never log or expose the secret
- Use `openssl rand -hex 32` to generate

## Redis Hardening
- Enable Redis AUTH with a strong password
- Use Redis over TLS (stunnel or Redis 6+ TLS)
- Bind to localhost or private network only
- Set `maxmemory-policy allkeys-lru` to prevent OOM

## Token Binding
- Tokens are bound to the requesting IP
- Validate `payload.Ip == requestIp` in your application
- Consider binding to User-Agent hash for stricter enforcement

## Challenge Freshness
- All challenge IDs are single-use; replay is prevented via Redis TTL + used-flag
- Reject solutions completed in < 100ms (too fast for human PoW)
- Reject solutions completed after expiry (> 5 minutes)

## Rate Limiting
- Per-IP sliding window in Redis prevents burst attacks
- Progressive difficulty increase punishes repeated failures
- Implement CIDR-level blocking for coordinated botnet attacks

## GDPR / Privacy
- Behavioral data (mouse, keyboard patterns) is sensitive — disclose in your Privacy Policy
- Do not persist raw behavioral events; only store the computed score
- Provide opt-out mechanism where required by law
- Behavioral data is processed in-memory and not stored after scoring

## Monitoring
- Log all failures with IP, timestamp, and reason
- Alert on sudden spikes in failure rate (may indicate an attack)
- Track false positive rate via a test account that always submits valid behavior
- Dashboard recommended: Grafana + Redis Exporter

## Defense in Depth
- PoW + Behavioral are primary layers; do not rely solely on image challenges
- Rotate challenge wordlists and puzzle variations regularly
- Advanced AI (GPT-4 Vision) can solve image CAPTCHAs — treat them as a secondary layer
