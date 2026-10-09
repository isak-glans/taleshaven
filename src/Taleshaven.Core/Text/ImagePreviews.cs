using System.Text.RegularExpressions;

namespace Taleshaven.Core.Text;

/// <summary>
/// Förhandsvisning av bilder som länkas i ett inlägg (B59). En länk över https vars adress slutar på .png, .jpg, .jpeg,
/// .gif eller .webp får bilden visad under sig i ett begränsat format. Bilden visas med ett vanligt &lt;img&gt;, som aldrig
/// kör skript, laddas först när den syns och skickar ingen referer. Markdowns bildsyntax <c>![text](adress)</c> blir en
/// sådan länk. Arbetar på redan renderad och sanerad HTML; det som läggs till är fasta element och den sanerade adressen.
/// </summary>
public static partial class ImagePreviews
{
    /// <summary>Högst så många bilder visas per inlägg, så att ett inlägg inte kan fyllas med bilder.</summary>
    public const int MaxPerPost = 5;

    /// <summary>Gör <c>![text](adress)</c> till en vanlig länk innan Markdown renderas; bildtaggar tas annars bort av saneringen.</summary>
    public static string PrepareMarkdown(string markdown) =>
        MarkdownImage().Replace(markdown, match =>
            $"[{(match.Groups["alt"].Value.Trim().Length > 0 ? match.Groups["alt"].Value : "image")}]({match.Groups["url"].Value})");

    public static string Apply(string html)
    {
        var count = 0;
        return ImageLink().Replace(html, match =>
        {
            if (++count > MaxPerPost)
                return match.Value;
            var url = match.Groups["url"].Value;
            return match.Value
                + $"""<span class="post-image"><a href="{url}" target="_blank" rel="nofollow noopener noreferrer">"""
                + $"""<img src="{url}" alt="" loading="lazy" decoding="async" referrerpolicy="no-referrer" /></a></span>""";
        });
    }

    // Länkar som saneringen har släppt igenom: href först, sedan target och rel. Bara https och bildfiler.
    [GeneratedRegex(
        """<a href="(?<url>https://[^"<>\s]+?\.(?:png|jpe?g|gif|webp)(?:\?[^"<>\s#]*)?)"[^>]*>.*?</a>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ImageLink();

    [GeneratedRegex(@"!\[(?<alt>[^\]\r\n]*)\]\((?<url>[^)\s]+)\)")]
    private static partial Regex MarkdownImage();
}
