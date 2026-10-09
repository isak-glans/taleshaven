using Taleshaven.Core.Text;
using Taleshaven.Infrastructure.Text;

namespace Taleshaven.Tests.Text;

/// <summary>Förhandsvisning av länkade bilder (B59).</summary>
public class ImagePreviewTests
{
    private readonly MarkdownRenderer renderer = new();

    private string Render(string markdown) => ImagePreviews.Apply(renderer.ToSafeHtml(ImagePreviews.PrepareMarkdown(markdown)));

    [Theory]
    [InlineData("https://example.com/map.png")]
    [InlineData("https://example.com/a/b/Map.JPG")]
    [InlineData("https://example.com/map.webp?size=large")]
    public void ImageLinksGetAPreview(string url)
    {
        var html = Render($"Look at [the map]({url}).");

        Assert.Contains("""<span class="post-image">""", html);
        Assert.Contains($"""<img src="{url.Replace("&", "&amp;")}" alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer" />""", html);
        Assert.Contains(">the map</a>", html);
    }

    [Theory]
    [InlineData("http://example.com/map.png")]              // bara https
    [InlineData("https://example.com/page.html")]           // bara bilder
    [InlineData("https://example.com/image.svg")]           // svg kan innehålla skript
    [InlineData("https://example.com/map.png.html")]
    [InlineData("javascript:alert(1)//.png")]
    public void OtherLinksGetNoPreview(string url)
    {
        Assert.DoesNotContain("<img", Render($"[link]({url})"));
    }

    [Fact]
    public void MarkdownImageSyntaxBecomesALinkWithPreview()
    {
        var html = Render("![The cellar](https://example.com/cellar.gif)");

        Assert.Contains(">The cellar</a>", html);
        Assert.Contains("""<img src="https://example.com/cellar.gif" """, html);
    }

    [Fact]
    public void ImageWithoutAltTextGetsALinkText()
    {
        Assert.Contains(">image</a>", Render("![](https://example.com/x.png)"));
    }

    [Fact]
    public void BareUrlsAlsoGetAPreview()
    {
        Assert.Contains("<img", Render("https://example.com/portrait.jpeg"));
    }

    [Fact]
    public void AtMostFivePreviewsPerPost()
    {
        var markdown = string.Join("\n\n", Enumerable.Range(1, 7).Select(i => $"[img {i}](https://example.com/{i}.png)"));

        Assert.Equal(ImagePreviews.MaxPerPost, Render(markdown).Split("<img").Length - 1);
    }

    [Fact]
    public void QuotesInTheUrlCannotBreakOut()
    {
        var html = Render("[x](https://example.com/a.png\"onerror=\"alert(1))");

        Assert.DoesNotContain("onerror=\"alert", html);
    }
}
