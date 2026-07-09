namespace CaptchaChallenges.Models;

public enum ChallengeType
{
    ImageText,
    PhysicsPuzzle,
    TemporalClick,
    LogicalReasoning
}

public class InteractiveChallenge
{
    public string ChallengeId { get; set; } = string.Empty;
    public ChallengeType Type { get; set; }
    public object Data { get; set; } = new();
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptsLeft { get; set; } = 2;
}

public class ImageChallenge
{
    public string ImageUrl { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public int InputLength { get; set; }
}

public class PhysicsPuzzleChallenge
{
    public string Instruction { get; set; } = string.Empty;
    public List<SvgElement> Elements { get; set; } = new();
    public string TargetElementId { get; set; } = string.Empty;
    public string TargetZoneId { get; set; } = string.Empty;
}

public class SvgElement
{
    public string Id { get; set; } = string.Empty;
    public string Shape { get; set; } = string.Empty; // circle, rect
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool IsDraggable { get; set; }
}

public class TemporalChallenge
{
    public string Instruction { get; set; } = string.Empty;
    public long TargetStartMs { get; set; }
    public long TargetEndMs { get; set; }
    public int AnimationDurationMs { get; set; }
}

public class LogicalChallenge
{
    public string Instruction { get; set; } = string.Empty;
    public List<LogicalOption> Options { get; set; } = new();
}

public class LogicalOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class ChallengeAnswer
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public long SolvedAtMs { get; set; }
    public string ClientIp { get; set; } = string.Empty;
}
