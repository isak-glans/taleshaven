using Taleshaven.Core.Text;

namespace Taleshaven.Core.Portraits;

/// <summary>
/// En bild i sajtens gemensamma bibliotek (B19): ett porträtt eller en ikon (<see cref="Kind"/>, B61). Administratörer
/// och managers laddar upp och sätter taggar; alla väljer porträtt till sina karaktärer och sin profil, och ikoner till
/// karaktärens status och tärningsslag. Taggarna lagras normaliserade (se <see cref="PortraitTags"/>).
/// </summary>
public class Portrait
{
    /// <summary>Längsta <see cref="ImportKey"/>, t.ex. "portraits/dwarves_1/7.8".</summary>
    public const int ImportKeyMaxLength = 120;

    private Portrait() { }

    public int Id { get; private set; }

    /// <summary>Nyckel till bilden i bildlagringen.</summary>
    public string ImageKey { get; private set; } = "";

    /// <summary>Porträtt eller ikon (B61). Styr i vilka väljare bilden visas.</summary>
    public ImageKind Kind { get; private set; }

    public List<string> Tags { get; private set; } = [];

    /// <summary>Var bilden kommer ifrån och under vilken licens, t.ex. "Egen bild, CC BY 4.0". Valfritt.</summary>
    public string? Source { get; private set; }

    public string UploadedById { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Var i ett källark bilden kommer ifrån, t.ex. "portraits/dwarves_1/7.8", för bilder som läggts in med importen (B64).
    /// Importen känner igen bilden på nyckeln och uppdaterar taggarna i stället för att lägga till den igen. Null för
    /// bilder som laddats upp på sidan.
    /// </summary>
    public string? ImportKey { get; private set; }

    public static Portrait Create(string imageKey, string uploadedById, ImageKind kind, string? tags, string? source, DateTimeOffset now,
        string? importKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedById);

        var portrait = new Portrait
        {
            ImageKey = imageKey,
            UploadedById = uploadedById,
            CreatedAt = now,
            ImportKey = importKey,
        };
        portrait.Update(kind, tags, source);
        return portrait;
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
