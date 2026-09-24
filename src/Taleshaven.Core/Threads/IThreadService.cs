using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

/// <summary>
/// Kampanjens chattkanaler (RPG och OOC) och deras inlägg. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IThreadService
{
    Task<ThreadDetails?> GetChannelAsync(int campaignId, ThreadKind kind, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Inläggen som visas när chatten öppnas (<see cref="ChatWindow"/>), äldst först.</summary>
    Task<PostPage> GetInitialPostsAsync(int threadId, CancellationToken cancellationToken = default);

    /// <summary>Nästa omgång inlägg som är äldre än <paramref name="beforePostId"/>, äldst först.</summary>
    Task<PostPage> GetPostsBeforeAsync(int threadId, long beforePostId, CancellationToken cancellationToken = default);

    /// <summary>Alla inlägg som är nyare än <paramref name="afterPostId"/>, äldst först.</summary>
    Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, CancellationToken cancellationToken = default);

    Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Slår tärningar på servern och sparar kastet som ett inlägg. Tillåtet endast i OOC (beslut B3).</summary>
    Task<PostItem> RollDiceAsync(int campaignId, int threadId, string userId, DiceNotation notation, string? label, CancellationToken cancellationToken = default);
}

public sealed record ThreadDetails(
    int Id,
    int CampaignId,
    ThreadKind Kind,
    ThreadStatus Status,
    bool CanWrite);

/// <summary>En sammanhängande följd inlägg, äldst först, och om det finns äldre inlägg före dem.</summary>
public sealed record PostPage(IReadOnlyList<PostItem> Posts, bool HasOlder);

public sealed record PostItem(
    long Id,
    string AuthorId,
    string AuthorName,
    bool AuthorIsGameMaster,
    string Content,
    DateTimeOffset CreatedAt,
    DiceRollView? Roll = null);

public sealed record DiceRollView(string Notation, string? Label, IReadOnlyList<int> Results, int Sides, int Modifier, int Total)
{
    public static DiceRollView From(DiceRoll roll) =>
        new(roll.Notation, roll.Label, roll.Results.ToList(), roll.Sides, roll.Modifier, roll.Total);
}
