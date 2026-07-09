using CaptchaChallenges.Models;
using System.Security.Cryptography;

namespace CaptchaChallenges.Services;

public interface ITemporalChallengeService
{
    (TemporalChallenge Challenge, long TargetStart, long TargetEnd) Generate();
    bool VerifyTiming(long targetStart, long targetEnd, long clickedAt);
}

public class TemporalChallengeService : ITemporalChallengeService
{
    private static readonly string[] Instructions =
    {
        "Click the moving target when it turns green",
        "Click the button when it reaches the center",
        "Tap the circle when it glows",
        "Press when the bar fills completely"
    };

    public (TemporalChallenge Challenge, long TargetStart, long TargetEnd) Generate()
    {
        var rng = new Random();
        int totalDurationMs = rng.Next(2000, 5000);
        int windowStart = rng.Next(500, totalDurationMs - 800);
        int windowDuration = rng.Next(400, 900);

        var instruction = Instructions[RandomNumberGenerator.GetInt32(Instructions.Length)];

        var challenge = new TemporalChallenge
        {
            Instruction = instruction,
            TargetStartMs = windowStart,
            TargetEndMs = windowStart + windowDuration,
            AnimationDurationMs = totalDurationMs
        };

        return (challenge, windowStart, windowStart + windowDuration);
    }

    public bool VerifyTiming(long targetStart, long targetEnd, long clickedAt)
    {
        const int toleranceMs = 150;
        return clickedAt >= targetStart - toleranceMs && clickedAt <= targetEnd + toleranceMs;
    }
}
