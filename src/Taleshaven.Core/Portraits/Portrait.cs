namespace Taleshaven.Core.Portraits;

/// <summary>
/// Ett porträtt i sajtens gemensamma bibliotek (B19). Administratörer och managers laddar upp och sätter taggar;
/// alla väljer porträtt ur biblioteket till sina karaktärer. Taggarna lagras normaliserade (se <see cref="PortraitTags"/>).
/// </summary>
public class Portrait
{
    private Portrait() { }

    public int Id { get; private set; }

    /// <summary>Nyckel till bilden i bildlagringen.</summary>
    public string ImageKey { get; private set; } = "";

    public List<string> Tags { get; private set; } = [];

    /// <summary>Var bilden kommer ifrån och under vilken licens, t.ex. "Egen bild, CC BY 4.0". Valfritt.</summary>
    public string? Source { get; private set; }

    public string UploadedById { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static Portrait Create(string imageKey, string uploadedById, string? tags, string? source, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedById);

        var portrait = new Portrait
        {
            ImageKey = imageKey,
            UploadedById = uploadedById,
            CreatedAt = now,
        };
        portrait.Update(tags, source);
        return portrait;
    }

    public void Update(string? tags, string? source)
    {
        var parsed = PortraitTags.Parse(tags);

        var trimmedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        if (trimmedSource?.Length > PortraitTags.SourceMaxLength)
            throw new CampaignRuleException($"Källa och licens får vara högst {PortraitTags.SourceMaxLength} tecken.");

        Tags = [.. parsed];
        Source = trimmedSource;
    }
}

/// <summary>Taggar på porträtt: små bokstäver, utan #, bara bokstäver, siffror och bindestreck (PB-2).</summary>
public static class PortraitTags
{
    public const int TagMaxLength = 30;
    public const int MaxTags = 20;
    public const int SourceMaxLength = 300;

    private static readonly char[] Separators = [' ', ',', '\t', '\r', '\n'];

    /// <summary>
    /// Tolkar taggar skrivna som "#dvärg #krigare" eller "dvärg, krigare". Dubbletter tas bort och ordningen behålls.
    /// Kastar <see cref="CampaignRuleException"/> om ingen tagg anges eller om en tagg är ogiltig.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? input)
    {
        var tags = new List<string>();
        foreach (var tag in Split(input))
        {
            if (tag.Length > TagMaxLength)
                throw new CampaignRuleException($"Taggen \"{tag}\" är för lång (högst {TagMaxLength} tecken).");
            if (!tag.All(c => char.IsLetterOrDigit(c) || c == '-'))
                throw new CampaignRuleException($"Taggen \"{tag}\" får bara innehålla bokstäver, siffror och bindestreck.");
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        if (tags.Count == 0)
            throw new CampaignRuleException("Ange minst en tagg, t.ex. #dvärg #krigare.");
        if (tags.Count > MaxTags)
            throw new CampaignRuleException($"Ett porträtt kan ha högst {MaxTags} taggar.");

        return tags;
    }

    /// <summary>Sökorden i väljaren, normaliserade som taggar. Ogiltiga tecken ignoreras i stället för att ge fel.</summary>
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
