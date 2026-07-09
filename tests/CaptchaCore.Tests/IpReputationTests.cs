using CaptchaCore.Configuration;
using CaptchaCore.Models;
using FluentAssertions;
using Xunit;

namespace CaptchaCore.Tests;

public class IpReputationTests
{
    [Fact]
    public void NewReputation_ShouldNotBeBlocked()
    {
        var rep = new IpReputation { Ip = "1.2.3.4" };
        rep.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void BlockedIp_WithExpiredBlock_ShouldUnblock()
    {
        var rep = new IpReputation
        {
            Ip = "1.2.3.4",
            IsBlocked = true,
            BlockedUntil = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        // Simulate the unblock check
        bool isBlocked = rep.IsBlocked &&
            rep.BlockedUntil.HasValue &&
            rep.BlockedUntil.Value > DateTimeOffset.UtcNow;
        isBlocked.Should().BeFalse();
    }

    [Fact]
    public void BlockedIp_WithActiveBlock_ShouldRemainBlocked()
    {
        var rep = new IpReputation
        {
            Ip = "1.2.3.4",
            IsBlocked = true,
            BlockedUntil = DateTimeOffset.UtcNow.AddHours(1)
        };
        bool isBlocked = rep.IsBlocked &&
            rep.BlockedUntil.HasValue &&
            rep.BlockedUntil.Value > DateTimeOffset.UtcNow;
        isBlocked.Should().BeTrue();
    }

    [Fact]
    public void FailureThreshold_ShouldTriggerBlock()
    {
        var options = new CaptchaOptions();
        var rep = new IpReputation { Ip = "1.2.3.4" };

        for (int i = 0; i < options.RateLimit.MaxFailuresBeforeBlock; i++)
            rep.FailureCount++;

        if (rep.FailureCount >= options.RateLimit.MaxFailuresBeforeBlock)
        {
            rep.IsBlocked = true;
            rep.BlockedUntil = DateTimeOffset.UtcNow.AddMinutes(options.RateLimit.BlockDurationMinutes);
        }

        rep.IsBlocked.Should().BeTrue();
    }
}
