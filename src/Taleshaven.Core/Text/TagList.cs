namespace Taleshaven.Core.Text;

/// <summary>
/// Taggar som skrivs "#dvärg #krigare" eller "dvärg, krigare" (porträtt B19, kampanjer B47). Taggar lagras med små
/// bokstäver, utan #, och får bara innehålla bokstäver, siffror och bindestreck.
/// </summary>
public static class TagList
{
    public const int TagMaxLength = 30;

    private static readonly char[] Separators = [' ', ',', '\t', '\r', '\n'];

    /// <summary>
    /// Tolkar taggarna. Dubbletter tas bort och ordningen behålls. Kastar <see cref="CampaignRuleException"/> om en tagg
    /// är ogiltig, om det är fler än <paramref name="maxTags"/>, eller om ingen anges och <paramref name="required"/> är satt.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? input, int maxTags, bool required, string owner)
    {
        var tags = new List<string>();
        foreach (var tag in Split(input))
        {
            if (tag.Length > TagMaxLength)
                throw new CampaignRuleException($"The tag \"{tag}\" is too long (at most {TagMaxLength} characters).");
            if (!tag.All(c => char.IsLetterOrDigit(c) || c == '-'))
                throw new CampaignRuleException($"The tag \"{tag}\" can only contain letters, digits and hyphens.");
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        if (tags.Count == 0 && required)
            throw new CampaignRuleException("Enter at least one tag, e.g. #dwarf #warrior.");
        if (tags.Count > maxTags)
            throw new CampaignRuleException($"{owner} can have at most {maxTags} tags.");

        return tags;
    }

    /// <summary>Sökord, normaliserade som taggar. Ogiltiga tecken ignoreras i stället för att ge fel.</summary>
    public static IReadOnlyList<string> ParseSearch(string? query) =>
        Split(query)
            .Select(term => new string(term.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray()))
            .Where(term => term.Length > 0)
            .Distinct()
            .ToList();

    /// <summary>Taggarna som text att visa eller redigera, t.ex. "#dvärg #krigare".</summary>
    public static string Format(IEnumerable<string> tags) => string.Join(" ", tags.Select(t => "#" + t));

    private static IEnumerable<string> Split(string? input) =>
        (input ?? "")
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim().TrimStart('#').ToLowerInvariant())
            .Where(part => part.Length > 0);
}
