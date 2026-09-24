namespace Taleshaven.Core.Chronicle;

/// <summary>
/// Kampanjens krönika. Alla inloggade får läsa; endast GM skriver (se avsnitt 4 i projektbeskrivningen).
/// Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IChronicleService
{
    /// <summary>En sida i krönikan med innehållsförteckning. Sidnummer utanför intervallet justeras till närmaste giltiga.</summary>
    Task<ChronicleView> GetPageAsync(int campaignId, int page, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Ett kapitel för redigering. Null om det inte finns i kampanjen.</summary>
    Task<ChapterView?> GetChapterAsync(int campaignId, int chapterId, CancellationToken cancellationToken = default);

    /// <summary>Lägger till ett kapitel sist i boken. Returnerar kapitlets nummer.</summary>
    Task<int> CreateChapterAsync(int campaignId, string userId, string? title, string? content, CancellationToken cancellationToken = default);

    /// <summary>Returnerar kapitlets nummer.</summary>
    Task<int> UpdateChapterAsync(int campaignId, int chapterId, string userId, string? title, string? content, CancellationToken cancellationToken = default);

    Task DeleteChapterAsync(int campaignId, int chapterId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Flyttar kapitlet ett steg (−1 = tidigare, +1 = senare). Returnerar kapitlets nya nummer.</summary>
    Task<int> MoveChapterAsync(int campaignId, int chapterId, string userId, int direction, CancellationToken cancellationToken = default);
}

public sealed record ChronicleView(
    IReadOnlyList<ChapterSummary> Contents,
    IReadOnlyList<ChapterView> Chapters,
    int Page,
    int PageCount,
    bool CanEdit)
{
    public int ChapterCount => Contents.Count;
}

public sealed record ChapterSummary(int Id, int Number, string Title)
{
    public int Page => ChronicleLimits.PageOf(Number);
}

public sealed record ChapterView(
    int Id,
    int Number,
    string Title,
    string Content,
    string AuthorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
