using CaptchaChallenges.Models;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text.Json;

namespace CaptchaChallenges.Services;

public interface IInteractiveChallengeFactory
{
    Task<InteractiveChallenge> CreateAsync(ChallengeType type);
    Task<bool> VerifyAsync(ChallengeAnswer answer);
}

public class InteractiveChallengeFactory : IInteractiveChallengeFactory
{
    private readonly IDatabase _redis;
    private readonly IImageChallengeService _imageService;
    private readonly IPhysicsChallengeService _physicsService;
    private readonly ITemporalChallengeService _temporalService;
    private readonly ILogicalChallengeService _logicalService;
    private readonly ILogger<InteractiveChallengeFactory> _logger;
    private const string Prefix = "captcha:interactive:";

    public InteractiveChallengeFactory(
        IConnectionMultiplexer redis,
        IImageChallengeService imageService,
        IPhysicsChallengeService physicsService,
        ITemporalChallengeService temporalService,
        ILogicalChallengeService logicalService,
        ILogger<InteractiveChallengeFactory> logger)
    {
        _redis = redis.GetDatabase();
        _imageService = imageService;
        _physicsService = physicsService;
        _temporalService = temporalService;
        _logicalService = logicalService;
        _logger = logger;
    }

    public async Task<InteractiveChallenge> CreateAsync(ChallengeType type)
    {
        var id = GenerateId();
        var expires = DateTimeOffset.UtcNow.AddSeconds(60);
        string answer;
        object data;

        switch (type)
        {
            case ChallengeType.ImageText:
                var (imgBytes, imgAnswer) = _imageService.GenerateImageChallenge();
                answer = imgAnswer.ToUpperInvariant();
                var imageB64 = Convert.ToBase64String(imgBytes);
                data = new ImageChallenge
                {
                    ImageUrl = $"data:image/png;base64,{imageB64}",
                    Instruction = "Type the text shown in the image",
                    InputLength = imgAnswer.Length
                };
                break;

            case ChallengeType.PhysicsPuzzle:
                var (physics, physicsAnswer) = _physicsService.Generate();
                answer = physicsAnswer;
                data = physics;
                break;

            case ChallengeType.TemporalClick:
                var (temporal, tStart, tEnd) = _temporalService.Generate();
                answer = $"{tStart}:{tEnd}";
                data = temporal;
                break;

            case ChallengeType.LogicalReasoning:
            default:
                var (logical, logicalAnswer) = _logicalService.Generate();
                answer = logicalAnswer;
                data = logical;
                break;
        }

        // Store answer server-side only
        var state = new { Answer = answer, ExpiresAt = expires, Attempts = 0 };
        await _redis.StringSetAsync(
            Prefix + id,
            JsonSerializer.Serialize(state),
            TimeSpan.FromSeconds(60));

        return new InteractiveChallenge
        {
            ChallengeId = id,
            Type = type,
            Data = data,
            ExpiresAt = expires,
            AttemptsLeft = 2
        };
    }

    public async Task<bool> VerifyAsync(ChallengeAnswer answer)
    {
        var key = Prefix + answer.ChallengeId;
        var json = await _redis.StringGetAsync(key);
        if (json.IsNullOrEmpty) return false;

        var state = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json!);
        if (state == null) return false;

        var storedAnswer = state["Answer"].GetString() ?? "";
        var expiresAt = state["ExpiresAt"].GetDateTimeOffset();
        var attempts = state["Attempts"].GetInt32();

        if (DateTimeOffset.UtcNow > expiresAt || attempts >= 2)
        {
            await _redis.KeyDeleteAsync(key);
            return false;
        }

        // Increment attempts
        var updated = new { Answer = storedAnswer, ExpiresAt = expiresAt, Attempts = attempts + 1 };
        await _redis.StringSetAsync(key, JsonSerializer.Serialize(updated), expiresAt - DateTimeOffset.UtcNow);

        bool correct = storedAnswer.Equals(answer.Answer.Trim(), StringComparison.OrdinalIgnoreCase);
        if (correct) await _redis.KeyDeleteAsync(key);

        return correct;
    }

    private static string GenerateId()
    {
        var b = new byte[16];
        RandomNumberGenerator.Fill(b);
        return Convert.ToHexString(b).ToLowerInvariant();
    }
}
