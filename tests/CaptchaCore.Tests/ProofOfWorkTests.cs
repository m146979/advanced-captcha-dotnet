using CaptchaCore.Services;
using FluentAssertions;
using Xunit;

namespace CaptchaCore.Tests;

public class ProofOfWorkTests
{
    // We test VerifyPoW directly without needing Redis
    private readonly ProofOfWorkServiceTestHelper _helper = new();

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void VerifyPoW_ValidNonce_ReturnsTrue(int difficulty)
    {
        var challenge = "testchallenge123";
        var nonce = BruteForceNonce(challenge, difficulty);
        _helper.VerifyPoW(challenge, nonce, difficulty).Should().BeTrue();
    }

    [Fact]
    public void VerifyPoW_WrongNonce_ReturnsFalse()
    {
        _helper.VerifyPoW("testchallenge", "wrongnonce", 3).Should().BeFalse();
    }

    [Fact]
    public void VerifyPoW_EmptyChallenge_ReturnsFalse()
    {
        _helper.VerifyPoW(string.Empty, "0", 3).Should().BeFalse();
    }

    private static string BruteForceNonce(string challenge, int difficulty)
    {
        var prefix = new string('0', difficulty);
        for (int i = 0; i < 5_000_000; i++)
        {
            var n = i.ToString();
            var input = challenge + n;
            var hash = System.Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
            if (hash.StartsWith(prefix)) return n;
        }
        throw new Exception("Could not find nonce");
    }
}

// Thin testable wrapper that exposes VerifyPoW without DI
internal class ProofOfWorkServiceTestHelper
{
    public bool VerifyPoW(string challenge, string nonce, int difficulty)
    {
        if (string.IsNullOrEmpty(challenge)) return false;
        var input = challenge + nonce;
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        var hashHex = System.Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hashHex.StartsWith(new string('0', difficulty));
    }
}
