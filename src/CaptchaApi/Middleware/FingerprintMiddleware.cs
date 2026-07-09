using System.Text.Json;

namespace CaptchaApi.Middleware;

public class FingerprintMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FingerprintMiddleware> _logger;

    private static readonly HashSet<string> SuspiciousUserAgents = new(StringComparer.OrdinalIgnoreCase)
    {
        "python-requests", "go-http-client", "curl", "wget", "httpie",
        "scrapy", "mechanize", "libwww-perl", "java/", "okhttp"
    };

    public FingerprintMiddleware(RequestDelegate next, ILogger<FingerprintMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        AnalyzeRequest(context);
        await _next(context);
    }

    private void AnalyzeRequest(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var acceptLang = context.Request.Headers.AcceptLanguage.ToString();

        var flags = new List<string>();

        if (string.IsNullOrEmpty(userAgent)) flags.Add("missing_ua");
        else if (SuspiciousUserAgents.Any(s => userAgent.Contains(s, StringComparison.OrdinalIgnoreCase)))
            flags.Add("bot_ua");

        if (string.IsNullOrEmpty(acceptLang)) flags.Add("missing_accept_language");
        if (!context.Request.Headers.ContainsKey("Accept")) flags.Add("missing_accept");
        if (!context.Request.Headers.ContainsKey("Accept-Encoding")) flags.Add("missing_accept_encoding");

        if (flags.Count > 0)
        {
            _logger.LogWarning("Suspicious request from {Ip}: {Flags}", ip, string.Join(", ", flags));
            context.Items["SuspiciousFlags"] = flags;
        }

        // Honeypot check: if X-Honeypot header is set, it's a bot
        if (context.Request.Headers.ContainsKey("X-Honeypot"))
        {
            _logger.LogWarning("Honeypot triggered from {Ip}", ip);
            context.Items["HoneypotTriggered"] = true;
        }
    }
}
