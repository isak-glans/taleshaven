using AngleSharp.Dom;
using Ganss.Xss;
using Markdig;
using Taleshaven.Core.Text;

namespace Taleshaven.Infrastructure.Text;

/// <summary>
/// Markdown → HTML i två steg: Markdig med rå HTML avstängd, sedan en strikt vitlista i HtmlSanitizer.
/// Båda är trådsäkra efter konfiguration, så klassen registreras som singleton.
/// </summary>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private static readonly string[] AllowedTags =
    [
        "p", "br", "strong", "em", "del", "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li", "blockquote", "a", "code", "pre", "hr",
    ];

    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseSoftlineBreakAsHardlineBreak()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .Build();

    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    public string ToSafeHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";

        var html = Markdown.ToHtml(markdown, pipeline);
        return sanitizer.Sanitize(html);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTags);

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "start"]);

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);

        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();
        sanitizer.AllowedClasses.Clear();

        // Länkar öppnas i ny flik och får inte komma åt sidan som öppnade dem.
        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is IElement { TagName: "A" } link)
            {
                link.SetAttribute("target", "_blank");
                link.SetAttribute("rel", "nofollow noopener noreferrer");
            }
        };

        return sanitizer;
    }
}
