using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;

namespace Taleshaven.Web.Components.Playroom;

/// <summary>
/// Renderar inläggstext till sanerad HTML, markerar OOC-text (B36) och sätter in tärningsslagen där de står i texten (B31).
/// Slagens HTML byggs här av sparade siffror och kodas, så den går inte att påverka via inläggets text.
/// </summary>
public static class PostHtml
{
    public static MarkupString Render(IMarkdownRenderer markdown, string content, IReadOnlyList<DiceRollView> rolls)
    {
        var html = OocMarkup.Apply(markdown.ToSafeHtml(content));
        return new MarkupString(InlineDice.ReplaceReferences(html, rolls.Count, number => Roll(rolls[number - 1])));
    }

    /// <summary>
    /// Förhandsgranskning: nya taggar visas som "rolls when posted", redan gjorda slag (vid redigering) med sina resultat.
    /// </summary>
    public static MarkupString Preview(IMarkdownRenderer markdown, string content, IReadOnlyList<DiceRollView>? rolls = null)
    {
        var html = OocMarkup.Apply(markdown.ToSafeHtml(content));
        html = InlineDice.ReplaceTags(html, notation =>
            $"""<span class="dice-inline dice-pending">🎲 <strong>{WebUtility.HtmlEncode(notation)}</strong> <em>rolls when posted</em></span>""");
        rolls ??= [];
        return new MarkupString(InlineDice.ReplaceReferences(html, rolls.Count, number => Roll(rolls[number - 1])));
    }

    private static string Roll(DiceRollView roll)
    {
        var html = new StringBuilder();
        html.Append($"""<span class="dice-inline" title="{WebUtility.HtmlEncode(Describe(roll))}">🎲 <strong class="dice-notation">{WebUtility.HtmlEncode(roll.Notation)}</strong> """);
        html.Append("""<span class="dice-values">""");
        foreach (var value in roll.Results)
            html.Append($"""<span class="dice-value {CriticalClass(roll, value)}">{value}</span>""");
        html.Append("</span>");
        if (roll.Modifier != 0)
            html.Append($" {(roll.Modifier > 0 ? "+" : "−")} {Math.Abs(roll.Modifier)}");
        html.Append($""" = <strong class="dice-total">{roll.Total}</strong></span>""");
        return html.ToString();
    }

    /// <summary>Slaget som text, t.ex. "2d6+3: [4, 5] + 3 = 12", för titlar och citat.</summary>
    public static string Describe(DiceRollView roll)
    {
        var modifier = roll.Modifier switch
        {
            > 0 => $" + {roll.Modifier}",
            < 0 => $" - {-roll.Modifier}",
            _ => "",
        };
        return $"{roll.Notation}: [{string.Join(", ", roll.Results)}]{modifier} = {roll.Total}";
    }

    // En naturlig 20 eller 1 på en ensam d20 markeras (T-4).
    private static string CriticalClass(DiceRollView roll, int value) => roll is { Sides: 20, Results.Count: 1 } && value is 20 or 1
        ? value == 20 ? "dice-max" : "dice-min"
        : "";
}
