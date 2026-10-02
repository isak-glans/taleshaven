using System.Text.RegularExpressions;

namespace Taleshaven.Core.Text;

/// <summary>En kort rad ren text ur Markdown, t.ex. början av en tråds introduktion i trådlistan (B27).</summary>
public static partial class TextExcerpt
{
    public static string From(string? markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";

        var text = MarkdownSymbols().Replace(markdown, "");
        text = Whitespace().Replace(text, " ").Trim();
        if (text.Length <= maxLength)
            return text;

        // Klipp vid ett ordslut när det går, så att inget ord kapas mitt i.
        var cut = text.LastIndexOf(' ', maxLength);
        return (cut > maxLength / 2 ? text[..cut] : text[..maxLength]).TrimEnd() + "…";
    }

    // Tärningsreferenser, rubrik- och citattecken i radbörjan samt betoning och kod.
    [GeneratedRegex(@"\[dice:\d+\]|\[/?dice\]|\[/?ooc\]|^\s{0,3}(#{1,6}|>)\s?|[*_`~]", RegexOptions.Multiline)]
    private static partial Regex MarkdownSymbols();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
