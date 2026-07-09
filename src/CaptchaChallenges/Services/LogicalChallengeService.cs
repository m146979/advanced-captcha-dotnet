using CaptchaChallenges.Models;
using System.Security.Cryptography;

namespace CaptchaChallenges.Services;

public interface ILogicalChallengeService
{
    (LogicalChallenge Challenge, string Answer) Generate();
}

public class LogicalChallengeService : ILogicalChallengeService
{
    // Each set: items in category + one odd one out (last is the answer)
    private static readonly List<(string Category, string[] Items, string OddOne)> Puzzles = new()
    {
        ("fruits",       new[] { "Apple", "Banana", "Cherry", "Car" },         "Car"),
        ("vehicles",     new[] { "Car", "Bus", "Truck", "Piano" },              "Piano"),
        ("animals",      new[] { "Dog", "Cat", "Fish", "Table" },               "Table"),
        ("colors",       new[] { "Red", "Blue", "Green", "Run" },               "Run"),
        ("numbers",      new[] { "2", "4", "8", "Dog" },                        "Dog"),
        ("shapes",       new[] { "Circle", "Square", "Triangle", "Sad" },       "Sad"),
        ("planets",      new[] { "Mars", "Venus", "Jupiter", "Monday" },        "Monday"),
        ("tools",        new[] { "Hammer", "Wrench", "Saw", "Cloud" },          "Cloud"),
        ("weather",      new[] { "Rain", "Snow", "Storm", "Chair" },            "Chair"),
        ("sports",       new[] { "Soccer", "Tennis", "Golf", "Milk" },          "Milk"),
    };

    public (LogicalChallenge Challenge, string Answer) Generate()
    {
        var puzzle = Puzzles[RandomNumberGenerator.GetInt32(Puzzles.Count)];
        var shuffled = puzzle.Items.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToList();

        var challenge = new LogicalChallenge
        {
            Instruction = "Click the item that does NOT belong in this group",
            Options = shuffled.Select((label, i) => new LogicalOption
            {
                Id = $"opt_{i}",
                Label = label
            }).ToList()
        };

        return (challenge, puzzle.OddOne);
    }
}
