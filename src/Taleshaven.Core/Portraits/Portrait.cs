using Taleshaven.Core.Text;

namespace Taleshaven.Core.Portraits;

/// <summary>
/// En bild i sajtens gemensamma bibliotek (B19): ett porträtt eller en ikon (<see cref="Kind"/>, B61). Administratörer
/// och managers laddar upp och sätter taggar; alla väljer porträtt till sina karaktärer och sin profil, och ikoner till
/// karaktärens status och tärningsslag. Taggarna lagras normaliserade (se <see cref="PortraitTags"/>).
/// </summary>
public class Portrait
{
    /// <summary>Längden på <see cref="ContentHash"/>: SHA-256 som 64 hexadecimala tecken.</summary>
    public const int ContentHashLength = 64;

    private Portrait() { }

    public int Id { get; private set; }

    /// <summary>Nyckel till bilden i bildlagringen.</summary>
    public string ImageKey { get; private set; } = "";

    /// <summary>Porträtt eller ikon (B61). Styr i vilka väljare bilden visas.</summary>
    public ImageKind Kind { get; private set; }

    public List<string> Tags { get; private set; } = [];

    /// <summary>Var bilden kommer ifrån och under vilken licens, t.ex. "Egen bild, CC BY 4.0". Valfritt.</summary>
    public string? Source { get; private set; }

    /// <summary>Vem som lade upp bilden; null för bilder som lagts in från ett manifest (B65).</summary>
    public string? UploadedById { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// SHA-256 av den sparade bilden (B65), så att samma bild inte läggs in två gånger från inkorgen. Null tills
    /// kontrollsumman har räknats ut (bilder från före B65 får den när appen startar).
    /// </summary>
    public string? ContentHash { get; private set; }

    public static Portrait Create(string imageKey, string? uploadedById, ImageKind kind, string? tags, string? source, DateTimeOffset now,
        string? contentHash = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageKey);

        var portrait = new Portrait
        {
            ImageKey = imageKey,
            UploadedById = uploadedById,
            CreatedAt = now,
        };
        portrait.SetContentHash(contentHash);
        portrait.Update(kind, tags, source);
        return portrait;
    }

    public void SetContentHash(string? contentHash)
    {
        if (contentHash is not null && (contentHash.Length != ContentHashLength || !contentHash.All(char.IsAsciiHexDigitLower)))
            throw new ArgumentException("Kontrollsumman ska vara SHA-256 med små hexadecimala tecken.", nameof(contentHash));
        ContentHash = contentHash;
    }

    public void Update(ImageKind kind, string? tags, string? source)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        var parsed = PortraitTags.Parse(tags);

        var trimmedSource = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        if (trimmedSource?.Length > PortraitTags.SourceMaxLength)
            throw new CampaignRuleException($"Source and licence can be at most {PortraitTags.SourceMaxLength} characters.");

        Kind = kind;
        Tags = [.. parsed];
        Source = trimmedSource;
    }
}

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ImageKind
{
    /// <summary>Runt porträtt av en person, varelse, ett djur eller föremål; till karaktärer, NPC:er och profiler.</summary>
    Portrait = 0,

    /// <summary>Liten ikon till räknare, tillstånd och tärningsslag (B58, B60). Visas aldrig som porträtt.</summary>
    Icon = 1,
}

/// <summary>Taggar på porträtt (PB-2). Reglerna är gemensamma med kampanjernas taggar, se <see cref="TagList"/>.</summary>
public static class PortraitTags
{
    public const int TagMaxLength = TagList.TagMaxLength;
    public const int MaxTags = 20;
    public const int SourceMaxLength = 300;

    /// <summary>
    /// Kategorierna som visas som filterknappar i porträttväljarna (B62), i den här ordningen, om någon bild har taggen.
    /// Taggstandarden står i projektbeskrivningen (B64).
    /// </summary>
    public static readonly IReadOnlyList<string> Categories = ["human", "elf", "dwarf", "monster", "animal", "object"];

    /// <summary>Taggknapparna i en väljare: kategorierna som finns först, sedan de vanligaste övriga taggarna.</summary>
    public static IReadOnlyList<string> Suggest(IEnumerable<TagCount> tags, int count)
    {
        var all = tags.ToList();
        var used = all.Select(t => t.Tag).ToHashSet();
        return Categories.Where(used.Contains)
            .Concat(all.Select(t => t.Tag).Where(t => !Categories.Contains(t)))
            .Take(count)
            .ToList();
    }

    /// <summary>Tolkar taggar skrivna som "#dvärg #krigare" eller "dvärg, krigare". Ett porträtt måste ha minst en tagg.</summary>
    public static IReadOnlyList<string> Parse(string? input) => TagList.Parse(input, MaxTags, required: true, "A portrait");

    /// <summary>Sökorden i väljaren, normaliserade som taggar.</summary>
    public static IReadOnlyList<string> ParseSearch(string? query) => TagList.ParseSearch(query);

    /// <summary>Taggarna som text att visa eller redigera, t.ex. "#dvärg #krigare".</summary>
    public static string Format(IEnumerable<string> tags) => TagList.Format(tags);
}
