using CaptchaBehavioral.Services;
using CaptchaChallenges.Models;
using CaptchaChallenges.Services;
using CaptchaCore.Models;
using CaptchaCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace CaptchaApi.Endpoints;

public static class CaptchaEndpoints
{
    public static void MapCaptchaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/captcha").WithTags("CAPTCHA");

        group.MapPost("/challenge", GetChallenge);
        group.MapPost("/verify", VerifyChallenge);
        group.MapPost("/interactive/get", GetInteractiveChallenge);
        group.MapPost("/interactive/verify", VerifyInteractiveChallenge);
        group.MapGet("/image/{id}", GetChallengeImage);
    }

    private static async Task<IResult> GetChallenge(
        [FromBody] ChallengeRequest request,
        HttpContext ctx,
        IProofOfWorkService powService,
        IRateLimitService rateLimitService,
        IIpReputationService reputationService)
    {
        request.ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!await rateLimitService.IsAllowedAsync(request.ClientIp))
            return Results.StatusCode(429);

        if (await reputationService.IsBlockedAsync(request.ClientIp))
            return Results.StatusCode(403);

        await rateLimitService.IncrementAsync(request.ClientIp);
        var challenge = await powService.GenerateChallengeAsync(request);
        return Results.Ok(challenge);
    }

    private static async Task<IResult> VerifyChallenge(
        [FromBody] VerifyRequest request,
        HttpContext ctx,
        IProofOfWorkService powService,
        ITokenService tokenService,
        IBehavioralAnalyzer behavioralAnalyzer,
        IIpReputationService reputationService,
        IConfiguration config)
    {
        request.ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var (valid, state) = await powService.ValidateSolutionAsync(request.ChallengeId, request.Solution);
        if (!valid || state == null)
        {
            await reputationService.RecordFailureAsync(request.ClientIp);
            return Results.Ok(new VerifyResponse { Success = false, Message = "Invalid or expired challenge" });
        }

        // Behavioral analysis
        double humanScore = 0.5;
        if (request.BehavioralData != null)
            humanScore = behavioralAnalyzer.Analyze(request.BehavioralData);

        var threshold = config.GetValue<double>("Captcha:BehavioralAnalysis:HumanConfidenceThreshold", 0.75);
        var suspicionThreshold = config.GetValue<double>("Captcha:BehavioralAnalysis:SuspicionThreshold", 0.5);

        await reputationService.RecordSuccessAsync(request.ClientIp);

        if (humanScore >= threshold)
        {
            var token = await tokenService.IssueTokenAsync(request.ClientIp, state.SessionId, humanScore);
            return Results.Ok(new VerifyResponse { Success = true, Token = token });
        }

        if (humanScore >= suspicionThreshold)
        {
            // Trigger interactive challenge
            var types = new[] { "ImageText", "PhysicsPuzzle", "TemporalClick", "LogicalReasoning" };
            var rndType = types[Random.Shared.Next(types.Length)];
            return Results.Ok(new VerifyResponse
            {
                Success = false,
                RequiresInteractiveChallenge = true,
                ChallengeType = rndType
            });
        }

        await reputationService.RecordFailureAsync(request.ClientIp);
        return Results.Ok(new VerifyResponse { Success = false, Message = "Behavioral analysis failed" });
    }

    private static async Task<IResult> GetInteractiveChallenge(
        [FromBody] InteractiveChallengeRequest request,
        IInteractiveChallengeFactory factory)
    {
        if (!Enum.TryParse<ChallengeType>(request.ChallengeType, true, out var type))
            type = ChallengeType.LogicalReasoning;

        var challenge = await factory.CreateAsync(type);
        return Results.Ok(challenge);
    }

    private static async Task<IResult> VerifyInteractiveChallenge(
        [FromBody] ChallengeAnswer answer,
        HttpContext ctx,
        IInteractiveChallengeFactory factory,
        ITokenService tokenService,
        IIpReputationService reputationService)
    {
        answer.ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var correct = await factory.VerifyAsync(answer);

        if (!correct)
        {
            await reputationService.RecordFailureAsync(answer.ClientIp);
            return Results.Ok(new VerifyResponse { Success = false, Message = "Incorrect answer" });
        }

        var token = await tokenService.IssueTokenAsync(answer.ClientIp, Guid.NewGuid().ToString(), 0.9);
        return Results.Ok(new VerifyResponse { Success = true, Token = token });
    }

    private static IResult GetChallengeImage(
        string id,
        IImageChallengeService imageService)
    {
        var bytes = imageService.RenderChallengeImage(id);
        return Results.File(bytes, "image/png",
            fileDownloadName: null,
            enableRangeProcessing: false);
    }
}

public class InteractiveChallengeRequest
{
    public string ChallengeId { get; set; } = string.Empty;
    public string ChallengeType { get; set; } = string.Empty;
}
