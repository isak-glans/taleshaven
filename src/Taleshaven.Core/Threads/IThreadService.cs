using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

/// <summary>
/// Kampanjens trådar och deras inlägg (B25–B33). Allt läses för en viss användare (viewerId): dolda NPC:er visas med
/// riktigt namn bara för kampanjens GM (B16), och behörigheterna per inlägg räknas ut för den som läser.
/// Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IThreadService
{
    /// <summary>Trådlistan (B27): aktiva först, sedan avslutade, var för sig i GM:s ordning.</summary>
    Task<IReadOnlyList<ThreadSummary>> GetThreadsAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default);

    Task<ThreadDetails?> GetThreadAsync(int campaignId, int threadId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>En sida med inlägg (B28), äldst först. Sidnumret begränsas till trådens sidor.</summary>
    Task<PostPage> GetPostsPageAsync(int threadId, int page, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Sidan där inlägget står, eller null om det inte finns i tråden.</summary>
    Task<int?> GetPageOfPostAsync(int threadId, long postId, CancellationToken cancellationToken = default);

    /// <summary>Inlägg nyare än <paramref name="afterPostId"/>, för att lägga till dem direkt på sista sidan.</summary>
    Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, string viewerId, CancellationToken cancellationToken = default);

    Task<PostItem?> GetPostAsync(int threadId, long postId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publicerar ett inlägg, valfritt som en karaktär (B29, B37). Tärningstaggar i texten slås här (B31).
    /// </summary>
    Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content,
        int? characterId = null, long? replyToPostId = null, CancellationToken cancellationToken = default);

    /// <summary>Byter texten i ett inlägg. Den tidigare versionen sparas som historik.</summary>
    Task<PostItem> EditPostAsync(int campaignId, long postId, string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Tar bort ett inlägg mjukt (B30).</summary>
    Task DeletePostAsync(int campaignId, long postId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vem användaren senast skrev som i tråden (B37): <see cref="PostingChoice.HasPosted"/> är false om hen inte har skrivit där,
    /// annars är <see cref="PostingChoice.CharacterId"/> karaktären, eller null för "som sig själv".
    /// </summary>
    Task<PostingChoice> GetLastPostingChoiceAsync(int threadId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Skapar en tråd sist i listan (B25). Returnerar id.</summary>
    Task<int> CreateThreadAsync(int campaignId, string userId, string? title, CancellationToken cancellationToken = default);

    Task RenameThreadAsync(int campaignId, int threadId, string userId, string? title, CancellationToken cancellationToken = default);

    /// <summary>Flyttar tråden ett steg upp (-1) eller ner (+1) bland trådarna med samma status (B27).</summary>
    Task MoveThreadAsync(int campaignId, int threadId, string userId, int direction, CancellationToken cancellationToken = default);

    /// <summary>Avslutar tråden; spelarna kan inte längre skriva i den (B32).</summary>
    Task CompleteThreadAsync(int campaignId, int threadId, string userId, CancellationToken cancellationToken = default);

    Task ReopenThreadAsync(int campaignId, int threadId, string userId, CancellationToken cancellationToken = default);
}

public sealed record PostingChoice(bool HasPosted, int? CharacterId);

/// <summary>En rad i trådlistan (B27, B37).</summary>
public sealed record ThreadSummary(
    int Id,
    string Title,
    ThreadStatus Status,
    int PostCount,
    int ParticipantCount,
    PostAuthor? LastPostAuthor,
    DateTimeOffset? LastPostAt,
    int UnreadCount);

public sealed record ThreadDetails(
    int Id,
    int CampaignId,
    ThreadStatus Status,
    string Title,
    DateTimeOffset CreatedAt,
    int PostCount,
    int ParticipantCount,
    bool CanWrite,
    bool CanManage);

/// <summary>En sida med inlägg. <see cref="Page"/> är den sida som faktiskt visas (1-baserad).</summary>
public sealed record PostPage(IReadOnlyList<PostItem> Posts, int Page, int PageCount, int TotalCount);

public sealed record PostItem(
    long Id,
    int ThreadId,
    string AuthorId,
    string AuthorName,
    bool AuthorIsGameMaster,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    IReadOnlyList<DiceRollView> Rolls,
    PostCharacter? Character,
    PostReference? ReplyTo,
    bool IsDeleted,
    bool CanEdit,
    bool CanDelete);

/// <summary>Namnet som visas för ett inlägg: karaktären om det är skrivet som en, annars användaren.</summary>
public sealed record PostAuthor(string Name, string? AvatarUrl, string ColorKey, bool IsHiddenNpc);

/// <summary>Inlägget som ett svar pekar på (B30).</summary>
public sealed record PostReference(long PostId, string Name);

/// <summary>
/// Karaktären ett inlägg är skrivet som. För en dold NPC (B16) är <see cref="IsHidden"/> satt: GM får riktigt namn,
/// porträtt och <see cref="Alias"/>; alla andra får bara aliaset och ingen bild.
/// </summary>
public sealed record PostCharacter(int Id, string Name, bool IsNpc, string? AvatarUrl, bool IsHidden = false, string? Alias = null)
{
    public static PostCharacter ForViewer(int id, string name, bool isNpc, string? avatarUrl, bool isHidden, string? alias, bool viewerIsGameMaster)
    {
        if (!isHidden)
            return new(id, name, isNpc, avatarUrl);

        return viewerIsGameMaster
            ? new(id, name, isNpc, avatarUrl, IsHidden: true, Alias: alias)
            : new(id, Characters.Character.NameForPlayers(name, isHidden, alias), isNpc, AvatarUrl: null, IsHidden: true);
    }
}

public sealed record DiceRollView(string Notation, string? Label, IReadOnlyList<int> Results, int Sides, int Modifier, int Total)
{
    public static DiceRollView From(DiceRoll roll) =>
        new(roll.Notation, roll.Label, roll.Results.ToList(), roll.Sides, roll.Modifier, roll.Total);
}
