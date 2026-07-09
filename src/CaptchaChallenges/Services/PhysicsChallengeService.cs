using CaptchaChallenges.Models;
using System.Security.Cryptography;

namespace CaptchaChallenges.Services;

public interface IPhysicsChallengeService
{
    (PhysicsPuzzleChallenge Challenge, string Answer) Generate();
}

public class PhysicsChallengeService : IPhysicsChallengeService
{
    private static readonly string[] Instructions =
    {
        "Drag the red ball into the heavier box",
        "Place the small ball into the box on the right",
        "Drop the circle into the darker container",
        "Move the round object to the largest box"
    };

    public (PhysicsPuzzleChallenge Challenge, string Answer) Generate()
    {
        var rng = new Random();
        var instruction = Instructions[RandomNumberGenerator.GetInt32(Instructions.Length)];

        double weight1 = rng.Next(1, 5);
        double weight2 = rng.Next(6, 12);
        string heavierBox = weight1 > weight2 ? "box1" : "box2";

        var elements = new List<SvgElement>
        {
            new()
            {
                Id = "ball", Shape = "circle", X = 180, Y = 150,
                Width = 30, Height = 30, Color = "#e74c3c",
                Label = "", Weight = 1, IsDraggable = true
            },
            new()
            {
                Id = "box1", Shape = "rect", X = 30, Y = 230,
                Width = 100, Height = 80, Color = "#3498db",
                Label = $"{weight1}kg", Weight = weight1, IsDraggable = false
            },
            new()
            {
                Id = "box2", Shape = "rect", X = 270, Y = 230,
                Width = 100, Height = 80, Color = "#2ecc71",
                Label = $"{weight2}kg", Weight = weight2, IsDraggable = false
            }
        };

        var challenge = new PhysicsPuzzleChallenge
        {
            Instruction = instruction,
            Elements = elements,
            TargetElementId = "ball",
            TargetZoneId = heavierBox
        };

        return (challenge, heavierBox);
    }
}
