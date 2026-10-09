using Taleshaven.Core.Text;

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
            throw new CampaignRuleException($"Source and licence can be at most {PortraitTags.SourceMaxLength} characters.");

        Tags = [.. parsed];
        Source = trimmedSource;
    }
}

/// <summary>Taggar på porträtt (PB-2). Reglerna är gemensamma med kampanjernas taggar, se <see cref="TagList"/>.</summary>
public static class PortraitTags
{
    public const int TagMaxLength = TagList.TagMaxLength;
    public const int MaxTags = 20;
    public const int SourceMaxLength = 300;

    /// <summary>Tolkar taggar skrivna som "#dvärg #krigare" eller "dvärg, krigare". Ett porträtt måste ha minst en tagg.</summary>
    public static IReadOnlyList<string> Parse(string? input) => TagList.Parse(input, MaxTags, required: true, "A portrait");

    /// <summary>Sökorden i väljaren, normaliserade som taggar.</summary>
    public static IReadOnlyList<string> ParseSearch(string? query) => TagList.ParseSearch(query);

    /// <summary>Taggarna som text att visa eller redigera, t.ex. "#dvärg #krigare".</summary>
    public static string Format(IEnumerable<string> tags) => TagList.Format(tags);
}
