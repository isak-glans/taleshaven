using SkiaSharp;
using Taleshaven.Core;
using Taleshaven.Core.Media;

namespace Taleshaven.Infrastructure.Media;

/// <summary>
/// Avkodar bilden, vrider den enligt EXIF, beskär till en centrerad kvadrat, skalar om och kodar om som WebP.
/// Omkodningen tar bort all metadata (t.ex. GPS-position) och allt som inte är bilddata.
/// </summary>
internal sealed class SkiaImageProcessor : IImageProcessor
{
    private const int WebpQuality = 85;

    public byte[] CreateAvatar(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        buffer.Position = 0;

        using var codec = SKCodec.Create(buffer)
            ?? throw new CampaignRuleException("Filen är ingen bild som kan läsas. Använd JPG, PNG eller WebP.");

        if (codec.Info.Width > ImageLimits.MaxSourceDimension || codec.Info.Height > ImageLimits.MaxSourceDimension)
            throw new CampaignRuleException($"Bilden är för stor. Den får vara högst {ImageLimits.MaxSourceDimension} bildpunkter bred och hög.");

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new CampaignRuleException("Bilden kunde inte läsas.");
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);

        // Centrerad kvadrat ur originalet.
        var side = Math.Min(oriented.Width, oriented.Height);
        var sourceRect = SKRect.Create((oriented.Width - side) / 2f, (oriented.Height - side) / 2f, side, side);
        var targetRect = SKRect.Create(0, 0, ImageLimits.AvatarSize, ImageLimits.AvatarSize);

        using var surface = SKSurface.Create(new SKImageInfo(ImageLimits.AvatarSize, ImageLimits.AvatarSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using (var image = SKImage.FromBitmap(oriented))
        {
            surface.Canvas.DrawImage(image, sourceRect, targetRect, new SKSamplingOptions(SKCubicResampler.Mitchell));
        }

        using var result = surface.Snapshot();
        using var data = result.Encode(SKEncodedImageFormat.Webp, WebpQuality)
            ?? throw new InvalidOperationException("Bilden kunde inte kodas som WebP.");
        return data.ToArray();
    }

    // Mobilkameror sparar ofta bilden liggande och anger rotationen i EXIF. Spegelvända varianter är ovanliga och
    // behandlas som motsvarande rotation.
    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        var degrees = origin switch
        {
            SKEncodedOrigin.BottomRight or SKEncodedOrigin.BottomLeft => 180,
            SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom => 90,
            SKEncodedOrigin.LeftBottom or SKEncodedOrigin.LeftTop => 270,
            _ => 0,
        };

        if (degrees == 0)
            return bitmap.Copy();

        var swap = degrees is 90 or 270;
        var rotated = new SKBitmap(swap ? bitmap.Height : bitmap.Width, swap ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2f, -bitmap.Height / 2f);
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        return rotated;
    }
}
