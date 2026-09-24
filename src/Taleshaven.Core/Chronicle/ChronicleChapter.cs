namespace Taleshaven.Core.Chronicle;

/// <summary>
/// Ett kapitel i kampanjens krönika (beslut B1). Kapitlets nummer är dess plats i boken (<see cref="Position"/>),
/// så att numret alltid följer ordningen när GM flyttar eller tar bort kapitel (arbetsförslag F14).
/// </summary>
public class ChronicleChapter
{
    private ChronicleChapter() { }

    public int Id { get; private set; }
    public int CampaignId { get; private set; }

    /// <summary>Plats i boken, 1, 2, 3 … utan luckor. Visas som kapitelnummer.</summary>
    public int Position { get; internal set; }

    public string Title { get; private set; } = "";

    /// <summary>Kapitlets text i Markdown.</summary>
    public string Content { get; private set; } = "";

    public string AuthorId { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ChronicleChapter Create(int campaignId, int position, string? title, string? content, string authorId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);

        var chapter = new ChronicleChapter
        {
            CampaignId = campaignId,
            Position = position,
            AuthorId = authorId,
            CreatedAt = now,
        };
        chapter.Update(title, content, now);
        return chapter;
    }

    public void Update(string? title, string? content, DateTimeOffset now)
    {
        title = title?.Trim() ?? "";
        if (title.Length == 0)
            throw new CampaignRuleException("Kapitlet måste ha en titel.");
        if (title.Length > ChronicleLimits.TitleMaxLength)
            throw new CampaignRuleException($"Titeln får vara högst {ChronicleLimits.TitleMaxLength} tecken.");

        content = content?.Trim() ?? "";
        if (content.Length == 0)
            throw new CampaignRuleException("Kapitlet är tomt.");
        if (content.Length > ChronicleLimits.ContentMaxLength)
            throw new CampaignRuleException($"Kapitlet får vara högst {ChronicleLimits.ContentMaxLength} tecken.");

        Title = title;
        Content = content;
        UpdatedAt = now;
    }
}

public static class ChronicleLimits
{
    public const int TitleMaxLength = 100;
    public const int ContentMaxLength = 5_000;

    /// <summary>Antal kapitel per sida när krönikan läses (beslut B10).</summary>
    public const int ChaptersPerPage = 5;

    public static int PageOf(int chapterNumber) => (chapterNumber - 1) / ChaptersPerPage + 1;

    public static int PageCount(int chapterCount) => Math.Max(1, (chapterCount + ChaptersPerPage - 1) / ChaptersPerPage);
}

/// <summary>
/// Regler för kapitlens ordning. Arbetar på alla kapitel i en kampanj och håller numreringen 1..n utan luckor.
/// </summary>
public static class ChronicleOrdering
{
    public static int NextPosition(IEnumerable<ChronicleChapter> chapters) =>
        chapters.Select(c => c.Position).DefaultIfEmpty(0).Max() + 1;

    /// <summary>Flyttar kapitlet ett steg (−1 = tidigare, +1 = senare). Returnerar false om det redan ligger först/sist.</summary>
    public static bool Move(IReadOnlyList<ChronicleChapter> chapters, ChronicleChapter chapter, int direction)
    {
        if (direction is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Riktningen måste vara -1 eller 1.");

        var ordered = Normalize(chapters);
        var index = ordered.IndexOf(chapter);
        if (index < 0)
            throw new CampaignRuleException("Kapitlet finns inte.");

        var target = index + direction;
        if (target < 0 || target >= ordered.Count)
            return false;

        (ordered[index].Position, ordered[target].Position) = (ordered[target].Position, ordered[index].Position);
        return true;
    }

    /// <summary>Tar bort kapitlet ur ordningen och numrerar om de som följer efter.</summary>
    public static void Remove(IReadOnlyList<ChronicleChapter> chapters, ChronicleChapter chapter)
    {
        var remaining = chapters.Where(c => !ReferenceEquals(c, chapter)).ToList();
        Normalize(remaining);
    }

    // Sorterar efter nuvarande plats och sätter platserna till 1..n.
    private static List<ChronicleChapter> Normalize(IEnumerable<ChronicleChapter> chapters)
    {
        var ordered = chapters.OrderBy(c => c.Position).ThenBy(c => c.Id).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Position = i + 1;
        return ordered;
    }
}
