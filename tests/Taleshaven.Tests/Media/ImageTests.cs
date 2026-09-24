using SkiaSharp;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Infrastructure.Media;

namespace Taleshaven.Tests.Media;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor processor = new();

    private static MemoryStream Image(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return new MemoryStream(data.ToArray());
    }

    [Theory]
    [InlineData(600, 300, SKEncodedImageFormat.Png)]
    [InlineData(300, 900, SKEncodedImageFormat.Jpeg)]
    [InlineData(100, 100, SKEncodedImageFormat.Webp)]
    public void CreatesSquareWebpAvatar(int width, int height, SKEncodedImageFormat format)
    {
        var avatar = processor.CreateAvatar(Image(width, height, format));

        using var codec = SKCodec.Create(new MemoryStream(avatar));
        Assert.NotNull(codec);
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal(ImageLimits.AvatarSize, codec.Info.Width);
        Assert.Equal(ImageLimits.AvatarSize, codec.Info.Height);
    }

    [Fact]
    public void RejectsNonImageData()
    {
        var notAnImage = new MemoryStream("<script>alert(1)</script>"u8.ToArray());

        Assert.Throws<CampaignRuleException>(() => processor.CreateAvatar(notAnImage));
    }

    [Fact]
    public void RejectsHugeDimensions()
    {
        Assert.Throws<CampaignRuleException>(() => processor.CreateAvatar(Image(ImageLimits.MaxSourceDimension + 1, 2)));
    }
}

public class LocalImageStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "taleshaven-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task SavesOpensAndDeletesAvatar()
    {
        var store = new LocalImageStore(root);

        var key = await store.SaveAvatarAsync([1, 2, 3]);

        Assert.Matches("^[0-9a-f]{32}\\.webp$", key);
        using (var stream = store.OpenAvatar(key))
        {
            Assert.NotNull(stream);
            Assert.Equal(3, stream.Length);
        }

        store.DeleteAvatar(key);
        Assert.Null(store.OpenAvatar(key));
    }

    [Theory]
    [InlineData("../appsettings.json")]
    [InlineData("..\\..\\secrets.txt")]
    [InlineData("0123456789abcdef0123456789abcdef.exe")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.webp")]
    [InlineData("")]
    public void RejectsKeysThatAreNotGenerated(string key)
    {
        var store = new LocalImageStore(root);

        Assert.False(LocalImageStore.IsValidKey(key));
        Assert.Null(store.OpenAvatar(key));
    }
}
