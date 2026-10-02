using System.Text.RegularExpressions;

namespace Taleshaven.Core.Text;

/// <summary>
/// Hopfälld text i ett inlägg (B40). <c>[spoiler]…[/spoiler]</c> eller <c>[spoiler=Rubrik]…[/spoiler]</c> på egna rader blir en
/// ruta som är stängd tills läsaren öppnar den; mitt i en mening blir texten dold tills man klickar på den.
/// Texten är bara hopfälld, inte hemlig: den skickas till alla som får läsa inlägget.
/// Arbetar på redan renderad och sanerad HTML; rubriken är redan kodad och får inte innehålla &lt; &gt; eller ".
/// </summary>
public static partial class SpoilerMarkup
{
    public const string DefaultTitle = "Spoiler";

    public static string Apply(string html)
    {
        // Ett helt avsnitt, från början av ett stycke till slutet av samma eller ett senare stycke.
        html = BlockPattern().Replace(html, match =>
        {
            var title = match.Groups["title"].Success ? match.Groups["title"].Value.Trim() : "";
            var content = EmptyParagraph().Replace($"<p>{match.Groups["content"].Value}</p>", "");
            return $"""<details class="spoiler"><summary>{(title.Length > 0 ? title : DefaultTitle)}</summary><div class="spoiler-body">{content}</div></details>""";
        });

        // Mitt i ett stycke. Taggarna får inte spänna över stycken eller listpunkter.
        return InlinePattern().Replace(html, match =>
            $"""<span class="spoiler-inline" tabindex="0" title="Spoiler – click to show">{match.Groups["content"].Value}</span>""");
    }

    [GeneratedRegex(@"<p>\s*\[spoiler(?:=(?<title>[^\]\[<>""]{1,60}))?\]\s*(?:<br\s*/?>)?\s*(?<content>(?:(?!\[/?spoiler\b).)*?)\s*(?:<br\s*/?>)?\s*\[/spoiler\]\s*</p>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlockPattern();

    [GeneratedRegex(@"\[spoiler\](?<content>(?:(?!</?(?:p|li|ul|ol|blockquote|h\d|div|details)\b|\[/?spoiler\b).)+?)\[/spoiler\]",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlinePattern();

    [GeneratedRegex(@"<p>\s*</p>")]
    private static partial Regex EmptyParagraph();
}
