namespace CaptchaCore.Models;

public enum DifficultyLevel
{
    Easy = 3,
    Medium = 4,
    Hard = 5,
    VeryHard = 6
}

public class ChallengeRequest
{
    public string? SessionId { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string? Fingerprint { get; set; }
    public string ClientIp { get; set; } = string.Empty;
}

public class ChallengeResponse
{
    public string ChallengeId { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string PublicData { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class ChallengeState
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
}

public class VerifyRequest
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public BehavioralData? BehavioralData { get; set; }
    public string? Fingerprint { get; set; }
    public string ClientIp { get; set; } = string.Empty;
}

public class VerifyResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public bool RequiresInteractiveChallenge { get; set; }
    public string? ChallengeType { get; set; }
    public string? Message { get; set; }
}

public class BehavioralData
{
    public List<MouseEvent> MouseEvents { get; set; } = new();
    public List<KeyEvent> KeyEvents { get; set; } = new();
    public List<ScrollEvent> ScrollEvents { get; set; } = new();
    public long TimeOnPage { get; set; }
    public int FocusBlurCount { get; set; }
    public int TouchEventCount { get; set; }
}

public class MouseEvent
{
    public double X { get; set; }
    public double Y { get; set; }
    public long Timestamp { get; set; }
}

public class KeyEvent
{
    public long Timestamp { get; set; }
    public string KeyType { get; set; } = string.Empty; // keydown / keyup
}

public class ScrollEvent
{
    public double ScrollY { get; set; }
    public long Timestamp { get; set; }
    public double Velocity { get; set; }
}

public class IpReputation
{
    public string Ip { get; set; } = string.Empty;
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public bool IsBlocked { get; set; }
    public DateTimeOffset? BlockedUntil { get; set; }
}

public class CaptchaToken
{
    public string Jti { get; set; } = string.Empty;
    public long Iat { get; set; }
    public long Exp { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Sid { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Signature { get; set; } = string.Empty;
}
