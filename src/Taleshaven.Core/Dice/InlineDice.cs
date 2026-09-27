using System.Text.RegularExpressions;
using Taleshaven.Core.Threads;

namespace Taleshaven.Core.Dice;

/// <summary>
/// Tärningar mitt i ett inlägg (B31). <c>[dice]1d20+3[/dice]</c> slås när inlägget publiceras och byts i den sparade
/// texten mot en referens <c>[dice:1]</c> till det sparade slaget. Referenserna kan inte tas bort vid redigering, och
/// nya taggar slås bara när ett inlägg publiceras – så att ingen kan slå om tills resultatet blir bra.
/// </summary>
public static partial class InlineDice
{
    /// <summary>Taggen som man skriver, t.ex. för tärningsknappen i editorn.</summary>
    public static string Tag(string notation) => $"[dice]{notation}[/dice]";

    /// <summary>Referensen som står i den sparade texten, 1-baserad.</summary>
    public static string Reference(int number) => $"[dice:{number}]";

    public static bool ContainsTags(string? text) => text is not null && TagPattern().IsMatch(text);

    /// <summary>
    /// Slår alla taggar i texten och returnerar texten med referenser i stället för taggar, och slagen i samma ordning.
    /// Kastar <see cref="CampaignRuleException"/> vid ogiltig notation eller fler än <see cref="ThreadLimits.MaxRollsPerPost"/> slag.
    /// </summary>
    public static (string Content, List<DiceRoll> Rolls) RollAll(string content, IDiceRoller roller)
    {
        var tags = TagPattern().Matches(content);
        if (tags.Count > ThreadLimits.MaxRollsPerPost)
            throw new CampaignRuleException($"A post can have at most {ThreadLimits.MaxRollsPerPost} dice rolls.");

        var rolls = new List<DiceRoll>();
        var result = TagPattern().Replace(content, match =>
        {
            if (!DiceNotation.TryParse(match.Groups["notation"].Value, out var notation, out var error))
                throw new CampaignRuleException($"{error} ({match.Value})");

            rolls.Add(DiceRoll.Roll(notation, null, roller));
            return Reference(rolls.Count);
        });

        return (result, rolls);
    }

    /// <summary>
    /// Kontrollerar en redigering av ett inlägg med <paramref name="rollCount"/> sparade slag: alla referenser måste finnas
    /// kvar, och nya taggar får inte läggas till.
    /// </summary>
    public static void EnsureValidEdit(string content, int rollCount)
    {
        if (ContainsTags(content))
            throw new CampaignRuleException("Dice are rolled when a post is published. Write a new post to roll again.");

        for (var number = 1; number <= rollCount; number++)
        {
            if (!content.Contains(Reference(number), StringComparison.Ordinal))
                throw new CampaignRuleException("Dice rolls can't be removed from a post.");
        }
    }

    /// <summary>
    /// Ersätter referenser i redan renderad (och sanerad) HTML. <paramref name="render"/> får slagets nummer (1-baserat)
    /// och ska returnera färdig HTML; okända nummer lämnas som text.
    /// </summary>
    public static string ReplaceReferences(string html, int rollCount, Func<int, string> render) =>
        ReferencePattern().Replace(html, match =>
        {
            var number = int.Parse(match.Groups["number"].Value);
            return number >= 1 && number <= rollCount ? render(number) : match.Value;
        });

    /// <summary>Ersätter taggar som inte slagits än i HTML, t.ex. i förhandsgranskningen ("rolls when posted").</summary>
    public static string ReplaceTags(string html, Func<string, string> render) =>
        TagPattern().Replace(html, match => render(match.Groups["notation"].Value.Trim()));

    [GeneratedRegex(@"\[dice\](?<notation>[^\[\]]{1,30})\[/dice\]", RegexOptions.IgnoreCase)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\[dice:(?<number>\d{1,2})\]")]
    private static partial Regex ReferencePattern();
}
