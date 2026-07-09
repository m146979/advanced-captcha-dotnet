using System.Text.Json;
using CaptchaCore.Configuration;
using CaptchaCore.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CaptchaCore.Services;

public interface IIpReputationService
{
    Task<IpReputation?> GetReputationAsync(string ip);
    Task RecordSuccessAsync(string ip);
    Task RecordFailureAsync(string ip);
    Task<bool> IsBlockedAsync(string ip);
    Task BlockIpAsync(string ip, TimeSpan duration);
}

public class IpReputationService : IIpReputationService
{
    private readonly IDatabase _redis;
    private readonly CaptchaOptions _options;
    private const string ReputationKeyPrefix = "captcha:reputation:";
    private const string BlocklistKeyPrefix = "captcha:blocklist:";

    public IpReputationService(IConnectionMultiplexer redis, IOptions<CaptchaOptions> options)
    {
        _redis = redis.GetDatabase();
        _options = options.Value;
    }

    public async Task<IpReputation?> GetReputationAsync(string ip)
    {
        var key = ReputationKeyPrefix + ip;
        var json = await _redis.StringGetAsync(key);
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<IpReputation>(json!);
    }

    public async Task RecordSuccessAsync(string ip)
    {
        var rep = await GetOrCreateAsync(ip);
        rep.SuccessCount++;
        rep.LastSeen = DateTimeOffset.UtcNow;
        await SaveAsync(rep);
    }

    public async Task RecordFailureAsync(string ip)
    {
        var rep = await GetOrCreateAsync(ip);
        rep.FailureCount++;
        rep.LastSeen = DateTimeOffset.UtcNow;

        if (rep.FailureCount >= _options.RateLimit.MaxFailuresBeforeBlock)
        {
            rep.IsBlocked = true;
            rep.BlockedUntil = DateTimeOffset.UtcNow.AddMinutes(_options.RateLimit.BlockDurationMinutes);
        }

        await SaveAsync(rep);
    }

    public async Task<bool> IsBlockedAsync(string ip)
    {
        var rep = await GetReputationAsync(ip);
        if (rep == null) return false;
        if (rep.IsBlocked && rep.BlockedUntil.HasValue && rep.BlockedUntil.Value < DateTimeOffset.UtcNow)
        {
            rep.IsBlocked = false;
            await SaveAsync(rep);
            return false;
        }
        return rep.IsBlocked;
    }

    public async Task BlockIpAsync(string ip, TimeSpan duration)
    {
        var rep = await GetOrCreateAsync(ip);
        rep.IsBlocked = true;
        rep.BlockedUntil = DateTimeOffset.UtcNow.Add(duration);
        await SaveAsync(rep);
    }

    private async Task<IpReputation> GetOrCreateAsync(string ip)
    {
        var rep = await GetReputationAsync(ip);
        return rep ?? new IpReputation { Ip = ip, LastSeen = DateTimeOffset.UtcNow };
    }

    private async Task SaveAsync(IpReputation rep)
    {
        var key = ReputationKeyPrefix + rep.Ip;
        var json = JsonSerializer.Serialize(rep);
        await _redis.StringSetAsync(key, json, TimeSpan.FromHours(24));
    }
}
