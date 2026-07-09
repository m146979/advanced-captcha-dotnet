using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CaptchaCore.Configuration;
using CaptchaCore.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CaptchaCore.Services;

public interface ITokenService
{
    Task<string> IssueTokenAsync(string ip, string sessionId, double score);
    Task<(bool Valid, CaptchaToken? Token)> ValidateTokenAsync(string token);
}

public class TokenService : ITokenService
{
    private readonly IDatabase _redis;
    private readonly CaptchaOptions _options;
    private const string UsedTokenPrefix = "captcha:usedtoken:";

    public TokenService(IConnectionMultiplexer redis, IOptions<CaptchaOptions> options)
    {
        _redis = redis.GetDatabase();
        _options = options.Value;
    }

    public async Task<string> IssueTokenAsync(string ip, string sessionId, double score)
    {
        var jti = GenerateSecureId();
        var now = DateTimeOffset.UtcNow;
        var payload = new CaptchaToken
        {
            Jti = jti,
            Iat = now.ToUnixTimeSeconds(),
            Exp = now.AddMinutes(_options.TokenExpiryMinutes).ToUnixTimeSeconds(),
            Ip = ip,
            Sid = sessionId,
            Score = score
        };

        var json = JsonSerializer.Serialize(payload);
        var signature = ComputeHmac(json);
        payload.Signature = signature;

        var tokenJson = JsonSerializer.Serialize(payload);
        var tokenBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenJson));

        // Store JTI to allow single-use validation
        await _redis.StringSetAsync(
            UsedTokenPrefix + jti,
            "1",
            TimeSpan.FromMinutes(_options.TokenExpiryMinutes + 1));

        return tokenBase64;
    }

    public async Task<(bool Valid, CaptchaToken? Token)> ValidateTokenAsync(string tokenBase64)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBase64));
            var token = JsonSerializer.Deserialize<CaptchaToken>(json);
            if (token == null) return (false, null);

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now > token.Exp) return (false, null);

            // Verify HMAC
            var payloadWithoutSig = new CaptchaToken
            {
                Jti = token.Jti, Iat = token.Iat, Exp = token.Exp,
                Ip = token.Ip, Sid = token.Sid, Score = token.Score
            };
            var expectedSig = ComputeHmac(JsonSerializer.Serialize(payloadWithoutSig));
            if (token.Signature != expectedSig) return (false, null);

            // Check JTI exists (not revoked)
            var exists = await _redis.KeyExistsAsync(UsedTokenPrefix + token.Jti);
            if (!exists) return (false, null);

            return (true, token);
        }
        catch
        {
            return (false, null);
        }
    }

    private string ComputeHmac(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.HmacSecret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateSecureId()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
