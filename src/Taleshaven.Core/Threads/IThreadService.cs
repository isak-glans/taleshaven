namespace Taleshaven.Core.Threads;

/// <summary>
/// RPG-trådar, OOC-kanalen och deras inlägg. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IThreadService
{
    Task<IReadOnlyList<ThreadListItem>> GetRpgThreadsAsync(int campaignId, CancellationToken cancellationToken = default);

    Task<int> CreateRpgThreadAsync(int campaignId, string userId, NewThread thread, CancellationToken cancellationToken = default);

    Task SetThreadLockedAsync(int campaignId, int threadId, string userId, bool locked, CancellationToken cancellationToken = default);

    Task<ThreadDetails?> GetThreadAsync(int campaignId, int threadId, string viewerId, CancellationToken cancellationToken = default);

    Task<ThreadDetails?> GetOocThreadAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>De senaste <paramref name="count"/> inläggen, äldst först.</summary>
    Task<IReadOnlyList<PostItem>> GetLatestPostsAsync(int threadId, int count, CancellationToken cancellationToken = default);

    /// <summary>Upp till <paramref name="count"/> inlägg som är äldre än <paramref name="beforePostId"/>, äldst först.</summary>
    Task<IReadOnlyList<PostItem>> GetPostsBeforeAsync(int threadId, long beforePostId, int count, CancellationToken cancellationToken = default);

    /// <summary>Alla inlägg som är nyare än <paramref name="afterPostId"/>, äldst först.</summary>
    Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, CancellationToken cancellationToken = default);

    Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content, CancellationToken cancellationToken = default);
}

public sealed record NewThread(string? Title, string? Description);

public sealed record ThreadListItem(
    int Id,
    string Title,
    string Description,
    ThreadStatus Status,
    int PostCount,
    DateTimeOffset? LastPostAt,
    DateTimeOffset CreatedAt);

public sealed record ThreadDetails(
    int Id,
    int CampaignId,
    ThreadKind Kind,
    string Title,
    string Description,
    ThreadStatus Status,
    bool CanWrite,
    bool CanManage);

public sealed record PostItem(
    long Id,
    string AuthorId,
    string AuthorName,
    bool AuthorIsGameMaster,
    string Content,
    DateTimeOffset CreatedAt);
