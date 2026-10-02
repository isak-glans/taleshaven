using Taleshaven.Core.Text;
using Taleshaven.Infrastructure.Text;

namespace Taleshaven.Tests.Text;

public class SpoilerMarkupTests
{
    private readonly MarkdownRenderer renderer = new();

    private string Render(string markdown) => OocMarkup.Apply(SpoilerMarkup.Apply(renderer.ToSafeHtml(markdown)));

    [Fact]
    public void BlockOnOwnLinesBecomesClosedBox()
    {
        var html = Render("Before.\n\n[spoiler=Map of the cistern]\nThe arch leads **down**.\n[/spoiler]\n\nAfter.");

        Assert.Contains("""<details class="spoiler"><summary>Map of the cistern</summary><div class="spoiler-body"><p>The arch leads <strong>down</strong>.</p></div></details>""", html);
        Assert.DoesNotContain("<details open", html);
        Assert.Contains("<p>Before.</p>", html);
        Assert.Contains("<p>After.</p>", html);
        Assert.DoesNotContain("[spoiler", html);
    }

    [Fact]
    public void BlockWithoutTitleIsCalledSpoiler()
    {
        Assert.Contains("<summary>Spoiler</summary>", Render("[spoiler]Hidden.[/spoiler]"));
    }

    [Fact]
    public void BlockCanSpanSeveralParagraphsAndLists()
    {
        var html = Render("[spoiler=Clues]\n\nFirst.\n\n- one\n- two\n\n[/spoiler]");

        Assert.Contains("<summary>Clues</summary>", html);
        Assert.Contains("<p>First.</p>", html);
        Assert.Contains("<li>one</li>", html);
        Assert.DoesNotContain("<p></p>", html);
    }

    [Fact]
    public void InlineSpoilerIsHiddenText()
    {
        var html = Render("The traitor is [spoiler]Hesper[/spoiler], of course.");

        Assert.Contains("""The traitor is <span class="spoiler-inline" tabindex="0" title="Spoiler – click to show">Hesper</span>, of course.""", html);
        Assert.DoesNotContain("<details", html);
    }

    [Fact]
    public void OocInsideSpoilerStillWorks()
    {
        var html = Render("[spoiler]\n[ooc]Only if you want to know.[/ooc]\n[/spoiler]");

        Assert.Contains("<details class=\"spoiler\">", html);
        Assert.Contains("ooc-block", html);
    }

    [Fact]
    public void TitleCannotInjectMarkup()
    {
        var html = Render("[spoiler=<b onclick=x>Boom</b>]Text[/spoiler]");

        Assert.DoesNotContain("<b", html);
        Assert.DoesNotContain("onclick=x>", html);
    }

    [Fact]
    public void UnclosedTagIsLeftAsText()
    {
        Assert.Contains("[spoiler]never closed", Render("[spoiler]never closed"));
    }
}
