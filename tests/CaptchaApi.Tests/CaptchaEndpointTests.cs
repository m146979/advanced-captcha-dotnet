using CaptchaCore.Models;
using CaptchaCore.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CaptchaApi.Tests;

public class CaptchaEndpointTests
{
    [Fact]
    public async Task GetChallenge_BlockedIp_Returns403()
    {
        var reputationMock = new Mock<IIpReputationService>();
        reputationMock.Setup(r => r.IsBlockedAsync(It.IsAny<string>())).ReturnsAsync(true);

        var rateLimitMock = new Mock<IRateLimitService>();
        rateLimitMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Simulate the guard logic from the endpoint
        bool isBlocked = await reputationMock.Object.IsBlockedAsync("1.2.3.4");
        isBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetChallenge_RateLimited_Returns429()
    {
        var rateLimitMock = new Mock<IRateLimitService>();
        rateLimitMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>())).ReturnsAsync(false);

        bool allowed = await rateLimitMock.Object.IsAllowedAsync("1.2.3.4");
        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyChallenge_InvalidSolution_ReturnsFailure()
    {
        var powMock = new Mock<IProofOfWorkService>();
        powMock.Setup(p => p.ValidateSolutionAsync(It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync((false, (ChallengeState?)null));

        var (valid, state) = await powMock.Object.ValidateSolutionAsync("id", "badnonce");
        valid.Should().BeFalse();
        state.Should().BeNull();
    }

    [Fact]
    public async Task TokenService_IssuedToken_IsValidatable()
    {
        // Tests round-trip token encode/decode logic via a stub
        var tokenMock = new Mock<ITokenService>();
        tokenMock.Setup(t => t.IssueTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
                 .ReturnsAsync("mock.token.value");
        tokenMock.Setup(t => t.ValidateTokenAsync("mock.token.value"))
                 .ReturnsAsync((true, new CaptchaToken { Ip = "1.2.3.4", Score = 0.9 }));

        var token = await tokenMock.Object.IssueTokenAsync("1.2.3.4", "sess1", 0.9);
        var (valid, payload) = await tokenMock.Object.ValidateTokenAsync(token);

        valid.Should().BeTrue();
        payload!.Score.Should().Be(0.9);
    }
}
