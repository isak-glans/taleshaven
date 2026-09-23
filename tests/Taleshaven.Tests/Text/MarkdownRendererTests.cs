using Taleshaven.Infrastructure.Text;

namespace Taleshaven.Tests.Text;

public class MarkdownRendererTests
{
    private readonly MarkdownRenderer renderer = new();

    [Fact]
    public void RendersBasicFormatting()
    {
        var html = renderer.ToSafeHtml("**fet** och *kursiv* och ~~struken~~");

        Assert.Contains("<strong>fet</strong>", html);
        Assert.Contains("<em>kursiv</em>", html);
        Assert.Contains("<del>struken</del>", html);
    }

    [Fact]
    public void RendersHeadingsListsAndQuotes()
    {
        var html = renderer.ToSafeHtml("### Rubrik\n\n- ett\n- två\n\n1. första\n\n> citat");

        Assert.Contains("<h3>Rubrik</h3>", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("<ol>", html);
        Assert.Contains("<blockquote>", html);
    }

    [Fact]
    public void SingleLineBreakBecomesBr()
    {
        var html = renderer.ToSafeHtml("Rad ett\nRad två");

        Assert.Contains("<br>", html);
    }

    [Fact]
    public void LinksOpenInNewTabWithSafeRel()
    {
        var html = renderer.ToSafeHtml("[Karaktärsblad](https://example.com/blad)");

        Assert.Contains("href=\"https://example.com/blad\"", html);
        Assert.Contains("target=\"_blank\"", html);
        Assert.Contains("rel=\"nofollow noopener noreferrer\"", html);
    }

    [Fact]
    public void BareUrlsBecomeLinks()
    {
        Assert.Contains("href=\"https://example.com\"", renderer.ToSafeHtml("Se https://example.com"));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<a href=\"javascript:alert(1)\">klicka</a>")]
    [InlineData("<iframe src=\"https://evil.example\"></iframe>")]
    [InlineData("<div style=\"position:fixed\">x</div>")]
    public void RawHtmlIsNeverRendered(string input)
    {
        var html = renderer.ToSafeHtml(input);

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<div", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a ", html, StringComparison.OrdinalIgnoreCase);
        // Rå HTML visas som escapad text.
        Assert.Contains("&lt;", html);
    }

    [Theory]
    [InlineData("[klicka](javascript:alert(1))")]
    [InlineData("[klicka](JaVaScRiPt:alert(1))")]
    [InlineData("[klicka](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)")]
    [InlineData("[klicka](vbscript:msgbox(1))")]
    public void DangerousLinkSchemesAreRemoved(string input)
    {
        var html = renderer.ToSafeHtml(input);

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vbscript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImagesAreNotAllowed()
    {
        Assert.DoesNotContain("<img", renderer.ToSafeHtml("![bild](https://example.com/a.png)"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInputGivesEmptyOutput(string? input)
    {
        Assert.Equal("", renderer.ToSafeHtml(input));
    }
}
