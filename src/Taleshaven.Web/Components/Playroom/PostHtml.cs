using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;

namespace Taleshaven.Web.Components.Playroom;

/// <summary>
/// Renderar inläggstext till sanerad HTML med spoilers (B40) och OOC-text (B36), och tärningsslagen som text för
/// citat och titlar (B42). Själva slaglistan under ett inlägg ritas av <see cref="DiceRolls"/>.
/// </summary>
public static class PostHtml
{
    public static MarkupString Render(IMarkdownRenderer markdown, string content) =>
        new(OocMarkup.Apply(SpoilerMarkup.Apply(markdown.ToSafeHtml(content))));

    /// <summary>Förhandsgranskning: texten och de slag som ska slås när inlägget publiceras ("rolls when posted").</summary>
    public static MarkupString Preview(IMarkdownRenderer markdown, string content, IReadOnlyList<RollRequest>? pendingRolls = null)
    {
        var html = new StringBuilder(Render(markdown, content).Value);
        var rolls = pendingRolls?.Where(r => !string.IsNullOrWhiteSpace(r.Notation)).ToList() ?? [];
        if (rolls.Count > 0)
        {
            html.Append("""<ul class="post-rolls post-rolls-pending">""");
            foreach (var roll in rolls)
            {
                html.Append("""<li class="post-roll">🎲 """);
                if (!string.IsNullOrWhiteSpace(roll.Label))
                    html.Append($"""<span class="roll-label">{WebUtility.HtmlEncode(roll.Label.Trim())}</span> """);
                html.Append($"""<span class="roll-notation">{WebUtility.HtmlEncode(roll.Notation!.Trim())}</span>""");
                if (roll.Mode != DiceMode.Normal)
                    html.Append($""" <span class="roll-mode">{ModeText(roll.Mode)}</span>""");
                html.Append(""" <em class="text-secondary">rolls when posted</em></li>""");
            }
            html.Append("</ul>");
        }
        return new MarkupString(html.ToString());
    }

    /// <summary>Slaget som text, t.ex. "Attack: 1d20+5 (advantage): [17, 4] + 5 = 22", för titlar och citat.</summary>
    public static string Describe(DiceRollView roll)
    {
        var modifier = roll.Modifier switch
        {
            > 0 => $" + {roll.Modifier}",
            < 0 => $" - {-roll.Modifier}",
            _ => "",
        };
        var label = roll.Label is null ? "" : $"{roll.Label}: ";
        var mode = roll.Mode == DiceMode.Normal ? "" : $" ({ModeText(roll.Mode).ToLowerInvariant()})";
        return $"{label}{roll.Notation}{mode}: [{string.Join(", ", roll.Results)}]{modifier} = {roll.Total}";
    }

    public static string ModeText(DiceMode mode) => mode switch
    {
        DiceMode.Advantage => "Advantage",
        DiceMode.Disadvantage => "Disadvantage",
        _ => "",
    };
}
