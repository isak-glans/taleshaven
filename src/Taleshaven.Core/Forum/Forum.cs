namespace Taleshaven.Core.Forum;

/// <summary>En kategori i forumet (B72), t.ex. "General". Trådarna är vanliga trådar med en kategori i stället för en kampanj.</summary>
public class ForumCategory
{
    private ForumCategory() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";

    /// <summary>Ordningen på forumsidan, lägst först.</summary>
    public int Position { get; private set; }

    public static ForumCategory Create(string name, string description, int position) =>
        new() { Name = name, Description = description, Position = position };
}

/// <summary>
/// Forumet (B72). Alla inloggade läser och svarar; bara administratörer och managers skapar trådar, fäster och låser dem.
/// I en låst tråd kan bara de skriva. Inläggen är vanliga inlägg (hämtas med <c>IThreadService</c>), så redigering,
/// borttagning, rapporter och moderering fungerar som i kampanjerna. Regelbrott ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IForumService
{
    /// <summary>Kategorierna med sina trådar: fästa först, sedan senaste aktivitet.</summary>
    Task<ForumOverview> GetForumAsync(string viewerId, CancellationToken cancellationToken = default);

    /// <summary>En forumtråd, eller null om den inte finns (eller är en kampanjtråd).</summary>
    Task<ForumThreadDetails?> GetThreadAsync(int threadId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Skapar en tråd med ett första inlägg. Bara administratörer och managers. Returnerar trådens id.</summary>
    Task<int> CreateThreadAsync(int categoryId, string userId, string? title, string? content, CancellationToken cancellationToken = default);

    /// <summary>Svarar i en tråd. Returnerar inläggets id.</summary>
    Task<long> ReplyAsync(int threadId, string userId, string? content, long? replyToPostId = null, CancellationToken cancellationToken = default);

    /// <summary>Författaren redigerar sitt inlägg, om tråden inte är låst och inlägget inte dolt.</summary>
    Task EditPostAsync(long postId, string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Författaren, administratörer och managers tar bort ett inlägg (det ligger kvar som "This post was deleted.").</summary>
    Task DeletePostAsync(long postId, string userId, CancellationToken cancellationToken = default);

    Task SetPinnedAsync(int threadId, string userId, bool pinned, CancellationToken cancellationToken = default);

    Task SetLockedAsync(int threadId, string userId, bool locked, CancellationToken cancellationToken = default);

    Task RenameThreadAsync(int threadId, string userId, string? title, CancellationToken cancellationToken = default);

    /// <summary>Tar bort tråden med alla inlägg. Bara administratörer och managers.</summary>
    Task DeleteThreadAsync(int threadId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>Forumsidan. <see cref="CanManage"/>: användaren får skapa, fästa och låsa trådar.</summary>
public sealed record ForumOverview(IReadOnlyList<ForumCategoryView> Categories, bool CanManage);

public sealed record ForumCategoryView(int Id, string Name, string Description, IReadOnlyList<ForumThreadSummary> Threads);

public sealed record ForumThreadSummary(
    int Id,
    string Title,
    bool IsPinned,
    bool IsLocked,
    int PostCount,
    int UnreadCount,
    string? LatestAuthorName,
    string? LatestAuthorAvatarUrl,
    DateTimeOffset? LatestAt);

/// <summary>En forumtråd. <see cref="CanReply"/>: inte låst, eller användaren får hantera forumet.</summary>
public sealed record ForumThreadDetails(
    int Id,
    int CategoryId,
    string CategoryName,
    string Title,
    bool IsPinned,
    bool IsLocked,
    int PostCount,
    bool CanReply,
    bool CanManage);

public static class ForumLinks
{
    public static string Forum => "forum";

    public static string Thread(int threadId, int? page = null) => page is > 1 ? $"forum/threads/{threadId}?page={page}" : $"forum/threads/{threadId}";

    public static string Post(int threadId, long postId) => $"forum/threads/{threadId}?post={postId}#post-{postId}";
}
