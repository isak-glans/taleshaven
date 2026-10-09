using System.Text.RegularExpressions;

namespace Taleshaven.Core.Portraits;

/// <summary>
/// Föreslår en ikon ur biblioteket (bilder med taggen <see cref="IconTag"/>) för ett namn, t.ex. en räknare, ett tillstånd
/// eller ett sparat slag (B58). "Shortsword" ger ikonen taggad <c>shortsword</c> om den finns, annars en som är taggad
/// <c>sword</c>. Jämförelsen bortser från stora och små bokstäver, mellanslag och bindestreck.
/// </summary>
public static partial class IconMatcher
{
    public const string IconTag = "icon";

    // Taggar som beskriver en kategori snarare än ett föremål ger för breda träffar ("Weapon attack" → vilket vapen som helst).
    private static readonly HashSet<string> GeneralTags =
    [
        IconTag, "weapon", "armor", "gear", "magic", "clothing", "light", "tool", "jewelry", "treasure", "book",
        "instrument", "bag", "money", "coins",
    ];

    /// <summary>
    /// Bästa ikonen för namnet, eller null om ingen passar. <paramref name="icons"/> är ikonernas id och taggar; den första
    /// taggen som inte är <c>icon</c> räknas som ikonens namn.
    /// </summary>
    public static int? Suggest(string? name, IEnumerable<(int Id, IReadOnlyList<string> Tags)> icons)
    {
        var words = Words(name);
        if (words.Count == 0)
            return null;
        var whole = string.Concat(words);
        var terms = words.Append(whole).Distinct().ToList();

        int? best = null;
        var bestScore = 0;
        foreach (var (id, tags) in icons.OrderBy(i => i.Id))
        {
            var primary = tags.FirstOrDefault(t => t != IconTag);
            foreach (var tag in tags)
            {
                if (GeneralTags.Contains(tag))
                    continue;
                var score = Score(Normalize(tag), terms, isPrimary: tag == primary);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = id;
                }
            }
        }
        return best;
    }

    // Exakt träff väger tyngst, sedan en tagg som ingår i ett ord ("sword" i "shortsword") och sist ett ord som är början
    // av en tagg ("spell" i "spellbook"). Längre träffar väger tyngre än kortare, och ikonens namn tyngre än övriga taggar.
    private static int Score(string tag, IReadOnlyList<string> terms, bool isPrimary)
    {
        var score = 0;
        foreach (var term in terms)
        {
            if (term == tag)
                score = Math.Max(score, 1000 + tag.Length);
            else if (tag.Length >= 3 && term.Contains(tag, StringComparison.Ordinal))
                score = Math.Max(score, 500 + tag.Length * 10);
            else if (term.Length >= 4 && tag.StartsWith(term, StringComparison.Ordinal))
                score = Math.Max(score, 200 + term.Length * 10);
        }
        return score > 0 && isPrimary ? score + 50 : score;
    }

    private static string Normalize(string text) => new(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static List<string> Words(string? name) =>
        WordSeparators().Split(name ?? "")
            .Select(Normalize)
            .Where(w => w.Length > 0)
            .ToList();

    // Ord skiljs åt av allt som inte är bokstäver, siffror eller apostrof ("Thieves' tools" blir "thieves" och "tools").
    [GeneratedRegex(@"[^\p{L}\p{N}']+")]
    private static partial Regex WordSeparators();
}
