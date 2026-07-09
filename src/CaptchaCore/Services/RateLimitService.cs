using CaptchaCore.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CaptchaCore.Services;

public interface IRateLimitService
{
    Task<bool> IsAllowedAsync(string ip);
    Task IncrementAsync(string ip);
}

public class RateLimitService : IRateLimitService
{
    private readonly IDatabase _redis;
    private readonly CaptchaOptions _options;
    private const string RateLimitKeyPrefix = "captcha:ratelimit:";

    public RateLimitService(IConnectionMultiplexer redis, IOptions<CaptchaOptions> options)
    {
        _redis = redis.GetDatabase();
        _options = options.Value;
    }

    public async Task<bool> IsAllowedAsync(string ip)
    {
        var key = RateLimitKeyPrefix + ip;
        var count = await _redis.StringGetAsync(key);
        if (count.IsNullOrEmpty) return true;
        return (int)count < _options.RateLimit.MaxRequestsPerMinute;
    }

    public async Task IncrementAsync(string ip)
    {
        var key = RateLimitKeyPrefix + ip;
        var tran = _redis.CreateTransaction();
        _ = tran.StringIncrementAsync(key);
        _ = tran.KeyExpireAsync(key, TimeSpan.FromMinutes(1));
        await tran.ExecuteAsync();
    }
}
