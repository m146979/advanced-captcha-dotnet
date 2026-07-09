using CaptchaApi.Endpoints;
using CaptchaApi.Middleware;
using CaptchaBehavioral.Services;
using CaptchaChallenges.Services;
using CaptchaCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCaptchaCore(builder.Configuration);

// Behavioral analysis
builder.Services.AddSingleton<IBehavioralAnalyzer, BehavioralAnalyzer>();

// Challenge services
builder.Services.AddSingleton<IImageChallengeService, ImageChallengeService>();
builder.Services.AddSingleton<IPhysicsChallengeService, PhysicsChallengeService>();
builder.Services.AddSingleton<ITemporalChallengeService, TemporalChallengeService>();
builder.Services.AddSingleton<ILogicalChallengeService, LogicalChallengeService>();
builder.Services.AddScoped<IInteractiveChallengeFactory, InteractiveChallengeFactory>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Advanced CAPTCHA API", Version = "v1" });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

// Fingerprinting middleware
app.UseMiddleware<FingerprintMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();

// Register endpoints
app.MapCaptchaEndpoints();

// Load ML model if present
var analyzer = app.Services.GetRequiredService<IBehavioralAnalyzer>();
var modelPath = Path.Combine("models", "behavioral_model.zip");
if (File.Exists(modelPath))
    await analyzer.LoadModelAsync(modelPath);

app.Run();
