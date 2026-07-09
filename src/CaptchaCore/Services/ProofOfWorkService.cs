using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CaptchaCore.Configuration;
using CaptchaCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CaptchaCore.Services;

public interface IProofOfWorkService
{
    Task<ChallengeResponse> GenerateChallengeAsync(ChallengeRequest request);
    Task<(bool Valid, ChallengeState? State)> ValidateSolutionAsync(string challengeId, string nonce);
    bool VerifyPoW(string challenge, string nonce, int difficulty);
}

public class ProofOfWorkService : IProofOfWorkService
{
    private readonly IDatabase _redis;
    private readonly CaptchaOptions _options;
    private readonly IIpReputationService _reputationService;
    private readonly ILogger<ProofOfWorkService> _logger;

    private const string ChallengeKeyPrefix = "captcha:challenge:";

    public ProofOfWorkService(
        IConnectionMultiplexer redis,
        IOptions<CaptchaOptions> options,
        IIpReputationService reputationService,
        ILogger<ProofOfWorkService> logger)
    {
        _redis = redis.GetDatabase();
        _options = options.Value;
        _reputationService = reputationService;
        _logger = logger;
    }

    public async Task<ChallengeResponse> GenerateChallengeAsync(ChallengeRequest request)
    {
        var reputation = await _reputationService.GetReputationAsync(request.ClientIp);
        var difficulty = CalculateDifficulty(reputation);

        var challengeId = GenerateSecureId();
        var challenge = GenerateSecureChallenge();
        var sessionId = request.SessionId ?? GenerateSecureId();

        var state = new ChallengeState
        {
            ChallengeId = challengeId,
            Challenge = challenge,
            Difficulty = difficulty,
            ClientIp = request.ClientIp,
            SessionId = sessionId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ChallengeExpiryMinutes),
            IsUsed = false
        };

        var key = ChallengeKeyPrefix + challengeId;
        var json = JsonSerializer.Serialize(state);
        await _redis.StringSetAsync(key, json, TimeSpan.FromMinutes(_options.ChallengeExpiryMinutes));

        _logger.LogInformation("Challenge {ChallengeId} generated for IP {Ip} with difficulty {Difficulty}",
            challengeId, request.ClientIp, difficulty);

        return new ChallengeResponse
        {
            ChallengeId = challengeId,
            Difficulty = difficulty,
            PublicData = challenge,
            ExpiresAt = state.ExpiresAt
        };
    }

    public async Task<(bool Valid, ChallengeState? State)> ValidateSolutionAsync(string challengeId, string nonce)
    {
        var key = ChallengeKeyPrefix + challengeId;
        var json = await _redis.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            _logger.LogWarning("Challenge {ChallengeId} not found or expired", challengeId);
            return (false, null);
        }

        var state = JsonSerializer.Deserialize<ChallengeState>(json!);
        if (state == null || state.IsUsed)
        {
            _logger.LogWarning("Challenge {ChallengeId} already used or invalid", challengeId);
            return (false, null);
        }

        if (DateTimeOffset.UtcNow > state.ExpiresAt)
        {
            await _redis.KeyDeleteAsync(key);
            return (false, null);
        }

        var isValid = VerifyPoW(state.Challenge, nonce, state.Difficulty);

        // Mark as used immediately to prevent replay
        state.IsUsed = true;
        await _redis.StringSetAsync(key, JsonSerializer.Serialize(state), TimeSpan.FromMinutes(1));

        return (isValid, state);
    }

    public bool VerifyPoW(string challenge, string nonce, int difficulty)
    {
        var input = challenge + nonce;
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var prefix = new string('0', difficulty);
        return hashHex.StartsWith(prefix);
    }

    private int CalculateDifficulty(IpReputation? reputation)
    {
        if (reputation == null) return _options.DefaultDifficulty;
        if (reputation.IsBlocked) return _options.MaxDifficulty;

        var diff = _options.DefaultDifficulty;
        if (reputation.FailureCount > 10) diff = Math.Min(diff + 2, _options.MaxDifficulty);
        else if (reputation.FailureCount > 5) diff = Math.Min(diff + 1, _options.MaxDifficulty);
        else if (reputation.SuccessCount > 20 && reputation.FailureCount == 0)
            diff = Math.Max(diff - 1, _options.MinDifficulty);

        return diff;
    }

    private static string GenerateSecureId()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateSecureChallenge()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
