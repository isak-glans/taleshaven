using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

/// <summary>
/// Kampanjens chattkanaler (RPG och OOC) och deras inlägg. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IThreadService
{
    Task<ThreadDetails?> GetChannelAsync(int campaignId, ThreadKind kind, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Inläggen som visas när chatten öppnas (<see cref="ChatWindow"/>), äldst först. Med <paramref name="lastReadPostId"/>
    /// kommer även olästa inlägg med, så att chatten kan öppnas vid det första olästa.</summary>
    Task<PostPage> GetInitialPostsAsync(int threadId, long? lastReadPostId = null, CancellationToken cancellationToken = default);

    /// <summary>Nästa omgång inlägg som är äldre än <paramref name="beforePostId"/>, äldst först.</summary>
    Task<PostPage> GetPostsBeforeAsync(int threadId, long beforePostId, CancellationToken cancellationToken = default);

    /// <summary>Alla inlägg som är nyare än <paramref name="afterPostId"/>, äldst först.</summary>
    Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, CancellationToken cancellationToken = default);

    /// <summary>Skriver ett inlägg, i RPG valfritt som en karaktär (<paramref name="characterId"/>).</summary>
    Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content, int? characterId = null, CancellationToken cancellationToken = default);

    /// <summary>Byter texten i ett eget inlägg. Den tidigare versionen sparas som historik. Tärningskast kan inte redigeras.</summary>
    Task<PostItem> EditPostAsync(int campaignId, long postId, string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Ett enskilt inlägg, t.ex. för att uppdatera vyn när någon annan har redigerat det.</summary>
    Task<PostItem?> GetPostAsync(int threadId, long postId, CancellationToken cancellationToken = default);

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
    DiceRollView? Roll = null,
    PostCharacter? Character = null,
    DateTimeOffset? EditedAt = null);

/// <summary>Karaktären ett RPG-inlägg är skrivet som.</summary>
public sealed record PostCharacter(int Id, string Name, bool IsNpc, string? AvatarUrl);

public sealed record DiceRollView(string Notation, string? Label, IReadOnlyList<int> Results, int Sides, int Modifier, int Total)
{
    public static DiceRollView From(DiceRoll roll) =>
        new(roll.Notation, roll.Label, roll.Results.ToList(), roll.Sides, roll.Modifier, roll.Total);
}
