using CaptchaCore.Services;

namespace CaptchaApi.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        if (context.Request.Path.StartsWithSegments("/api/captcha"))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!await rateLimitService.IsAllowedAsync(ip))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests" });
                return;
            }
        }

        await _next(context);
    }
}
