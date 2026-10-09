using System.Text.RegularExpressions;

namespace Taleshaven.Core.Portraits;

/// <summary>
/// Föreslår en ikon ur biblioteket (bilder av typen <see cref="ImageKind.Icon"/>, B61) för ett namn, t.ex. en räknare, ett tillstånd
/// eller ett sparat slag (B58). "Shortsword" ger ikonen taggad <c>shortsword</c> om den finns, annars en som är taggad
/// <c>sword</c>. Jämförelsen bortser från stora och små bokstäver, mellanslag och bindestreck.
/// </summary>
public static partial class IconMatcher
{
    // Taggar som beskriver en kategori snarare än ett föremål ger för breda träffar ("Weapon attack" → vilket vapen som helst).
    private static readonly HashSet<string> GeneralTags =
    [
        "weapon", "armor", "gear", "magic", "clothing", "light", "tool", "jewelry", "treasure", "book",
        "instrument", "bag", "money", "coins",
    ];

    /// <summary>
    /// Bästa ikonen för namnet, eller null om ingen passar. <paramref name="icons"/> är ikonernas id och taggar; den första
    /// taggen räknas som ikonens namn.
    /// </summary>
    public static int? Suggest(string? name, IEnumerable<(int Id, IReadOnlyList<string> Tags)> icons)
    {
        var words = Words(name);
        if (words.Count == 0)
            return null;
        // Orden får sin plats i namnet; hela namnet ihopskrivet räknas som första ordet.
        var terms = words.Select((word, index) => (Text: word, Position: index))
            .Prepend((Text: string.Concat(words), Position: 0))
            .DistinctBy(t => t.Text)
            .ToList();

        int? best = null;
        var bestScore = 0;
        foreach (var (id, tags) in icons.OrderBy(i => i.Id))
        {
            var primary = tags.FirstOrDefault();
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
    // Ett ord längre fram i namnet väger lite mindre, eftersom det första ordet oftast är saken och de följande beskriver
    // den: "Spear (damage)" ska få spjutet, inte skadeikonen.
    private const int PositionPenalty = 20;

    private static int Score(string tag, IReadOnlyList<(string Text, int Position)> terms, bool isPrimary)
    {
        var score = 0;
        foreach (var (term, position) in terms)
        {
            var match = 0;
            if (term == tag)
                match = 1000 + tag.Length;
            else if (tag.Length >= 3 && term.Contains(tag, StringComparison.Ordinal))
                match = 500 + tag.Length * 10;
            else if (term.Length >= 4 && tag.StartsWith(term, StringComparison.Ordinal))
                match = 200 + term.Length * 10;
            if (match > 0)
                score = Math.Max(score, match - position * PositionPenalty);
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
