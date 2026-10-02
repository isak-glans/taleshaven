using Taleshaven.Core.Text;
using Taleshaven.Infrastructure.Text;

namespace Taleshaven.Tests.Text;

public class OocMarkupTests
{
    private readonly MarkdownRenderer renderer = new();

    private string Render(string markdown) => OocMarkup.Apply(renderer.ToSafeHtml(markdown));

    [Fact]
    public void InlineOocIsMarked()
    {
        var html = Render("Sigrun draws her hammer. [ooc]brb, dinner[/ooc] Then she charges.");

        Assert.Contains("""<span class="ooc-text"><span class="ooc-label">OOC</span> brb, dinner</span>""", html);
        Assert.Contains("Sigrun draws her hammer.", html);
        Assert.DoesNotContain("[ooc]", html);
    }

    [Fact]
    public void WholeParagraphsBecomeAnOocBlock()
    {
        var html = Render("[ooc]First paragraph.\n\nSecond paragraph.[/ooc]\n\nBack in character.");

        Assert.Contains("""<div class="ooc-block"><span class="ooc-label">OOC</span><p>First paragraph.</p>""", html);
        Assert.Contains("Second paragraph.</p></div>", html);
        Assert.Contains("<p>Back in character.</p>", html);
    }

    [Fact]
    public void TwoInlineOocPartsInOneParagraphStaySeparate()
    {
        var html = Render("[ooc]one[/ooc] middle [ooc]two[/ooc]");

        Assert.Contains("OOC</span> one</span> middle <span class=\"ooc-text\">", html);
        Assert.DoesNotContain("ooc-block", html);
    }

    [Fact]
    public void UnclosedTagIsLeftAsText()
    {
        Assert.Contains("[ooc]not closed", Render("[ooc]not closed"));
    }

    [Fact]
    public void OocContentIsStillSanitized()
    {
        var html = Render("[ooc]<script>alert(1)</script>[/ooc]");

        Assert.DoesNotContain("<script>", html);
    }
}
