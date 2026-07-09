using SkiaSharp;
using System.Security.Cryptography;

namespace CaptchaChallenges.Services;

public interface IImageChallengeService
{
    (byte[] ImageBytes, string Answer) GenerateImageChallenge();
    byte[] RenderChallengeImage(string challengeId);
}

public class ImageChallengeService : IImageChallengeService
{
    private static readonly string[] WordList =
    {
        "CAPTCHA", "VERIFY", "HUMAN", "SECURE", "ACCESS",
        "LOGIN", "PORTAL", "BRIDGE", "CLOUD", "DELTA",
        "FORGE", "GRANT", "HARBOR", "INDEX", "JUNGLE"
    };

    private static readonly Dictionary<string, (byte[] Bytes, string Answer)> _cache = new();

    public (byte[] ImageBytes, string Answer) GenerateImageChallenge()
    {
        var answer = WordList[RandomNumberGenerator.GetInt32(WordList.Length)];
        var bytes = RenderText(answer);
        return (bytes, answer);
    }

    public byte[] RenderChallengeImage(string challengeId)
    {
        if (_cache.TryGetValue(challengeId, out var cached)) return cached.Bytes;
        var (bytes, _) = GenerateImageChallenge();
        return bytes;
    }

    private byte[] RenderText(string text)
    {
        const int width = 240, height = 80;
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var rng = new Random();

        // Perlin-like noise background
        using var noisePaint = new SKPaint { Color = SKColors.LightGray.WithAlpha(120), StrokeWidth = 1 };
        for (int i = 0; i < 60; i++)
        {
            canvas.DrawLine(
                rng.Next(0, width), rng.Next(0, height),
                rng.Next(0, width), rng.Next(0, height),
                noisePaint);
        }

        // Render each character with distortion
        float x = 15;
        for (int i = 0; i < text.Length; i++)
        {
            using var paint = new SKPaint
            {
                TextSize = rng.Next(28, 42),
                IsAntialias = true,
                Color = new SKColor(
                    (byte)rng.Next(0, 180),
                    (byte)rng.Next(0, 180),
                    (byte)rng.Next(0, 180)),
                Typeface = SKTypeface.Default
            };

            canvas.Save();
            float rotDeg = (float)(rng.NextDouble() * 40 - 20);
            float cy = height / 2f + (float)(rng.NextDouble() * 10 - 5);
            canvas.RotateDegrees(rotDeg, x + 12, cy);

            var scaleX = (float)(0.85 + rng.NextDouble() * 0.3);
            canvas.Scale(scaleX, 1);

            canvas.DrawText(text[i].ToString(), x / scaleX, cy + paint.TextSize / 3, paint);
            canvas.Restore();

            x += paint.TextSize * 0.55f + (float)(rng.NextDouble() * 4);
        }

        // Overlay noise dots
        using var dotPaint = new SKPaint { Color = SKColors.Gray.WithAlpha(100) };
        for (int i = 0; i < 200; i++)
            canvas.DrawCircle(rng.Next(0, width), rng.Next(0, height), 1, dotPaint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
