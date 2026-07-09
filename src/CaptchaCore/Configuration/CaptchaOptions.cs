namespace CaptchaCore.Configuration;

public class CaptchaOptions
{
    public const string SectionName = "Captcha";

    public int DefaultDifficulty { get; set; } = 4;
    public int MinDifficulty { get; set; } = 3;
    public int MaxDifficulty { get; set; } = 6;
    public int ChallengeExpiryMinutes { get; set; } = 5;
    public int TokenExpiryMinutes { get; set; } = 10;
    public string HmacSecret { get; set; } = string.Empty;

    public RateLimitOptions RateLimit { get; set; } = new();
    public BehavioralAnalysisOptions BehavioralAnalysis { get; set; } = new();
}

public class RateLimitOptions
{
    public int MaxRequestsPerMinute { get; set; } = 10;
    public int MaxFailuresBeforeBlock { get; set; } = 5;
    public int BlockDurationMinutes { get; set; } = 30;
}

public class BehavioralAnalysisOptions
{
    public int MinInteractionTime { get; set; } = 2000;
    public int RequiredMouseMovements { get; set; } = 5;
    public double SuspicionThreshold { get; set; } = 0.5;
    public double HumanConfidenceThreshold { get; set; } = 0.75;
}
