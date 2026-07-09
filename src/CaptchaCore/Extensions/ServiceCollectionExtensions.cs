using CaptchaCore.Configuration;
using CaptchaCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CaptchaCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCaptchaCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CaptchaOptions>(configuration.GetSection(CaptchaOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<IIpReputationService, IpReputationService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddScoped<IProofOfWorkService, ProofOfWorkService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
