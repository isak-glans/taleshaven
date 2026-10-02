using System.Text.RegularExpressions;

namespace Taleshaven.Core.Text;

/// <summary>
/// Text utanför rollspelet i ett inlägg (B36): <c>[ooc]…[/ooc]</c> visas tydligt avskild, så att den inte läses som
/// in character. Arbetar på redan renderad och sanerad HTML; det som läggs till är fasta element utan användardata.
/// </summary>
public static partial class OocMarkup
{
    private const string Label = """<span class="ooc-label">OOC</span>""";

    public static string Apply(string html)
    {
        // Ett helt avsnitt, från början av ett stycke till slutet av samma eller ett senare stycke.
        html = BlockPattern().Replace(html, match => $"""<div class="ooc-block">{Label}<p>{match.Groups["content"].Value}</p></div>""");

        // Mitt i ett stycke. Taggarna får inte spänna över stycken eller listpunkter.
        return InlinePattern().Replace(html, match => $"""<span class="ooc-text">{Label} {match.Groups["content"].Value}</span>""");
    }

    [GeneratedRegex(@"<p>\s*\[ooc\](?<content>(?:(?!\[/?ooc\]).)*?)\[/ooc\]\s*</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BlockPattern();

    [GeneratedRegex(@"\[ooc\](?<content>(?:(?!</?(?:p|li|ul|ol|blockquote|h\d|div)\b).)*?)\[/ooc\]", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlinePattern();
}
