namespace Taleshaven.Core.Messages;

/// <summary>
/// En privat konversation mellan två personer (B73). Meddelandena är vanliga inlägg i en egen tråd utan kampanj och
/// kategori (<see cref="ThreadId"/>), så redigering, borttagning, olästa, rapporter och dolda inlägg fungerar som i
/// trådarna. Bara de två deltagarna kan läsa den. Paret sparas i ordning (<see cref="UserAId"/> &lt; <see cref="UserBId"/>),
/// så att det finns högst en konversation per par.
/// </summary>
public class Conversation
{
    private Conversation() { }

    public int Id { get; private set; }
    public int ThreadId { get; private set; }
    public string UserAId { get; private set; } = "";
    public string UserBId { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static Conversation Create(int threadId, string userId, string otherUserId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(otherUserId);
        var (a, b) = Pair(userId, otherUserId);
        return new Conversation { ThreadId = threadId, UserAId = a, UserBId = b, CreatedAt = now };
    }

    /// <summary>Paret i den ordning det sparas.</summary>
    public static (string A, string B) Pair(string userId, string otherUserId) =>
        string.CompareOrdinal(userId, otherUserId) < 0 ? (userId, otherUserId) : (otherUserId, userId);

    public bool Includes(string userId) => UserAId == userId || UserBId == userId;

    public string OtherThan(string userId) => UserAId == userId ? UserBId : UserAId;
}

/// <summary>
/// En användare har blockerat en annan (B73): ingen av dem kan skicka meddelanden till den andra. Konversationen finns
/// kvar och kan läsas.
/// </summary>
public class UserBlock
{
    private UserBlock() { }

    public string BlockerId { get; private set; } = "";
    public string BlockedId { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static UserBlock Create(string blockerId, string blockedId, DateTimeOffset now) =>
        new() { BlockerId = blockerId, BlockedId = blockedId, CreatedAt = now };
}

/// <summary>
/// Privata meddelanden (B73): konversationer mellan två personer, och alla får skriva till alla som inte har blockerat
/// dem. Meddelandena hämtas med <c>IThreadService</c> med konversationens tråd; den släpper bara in deltagarna.
/// Regelbrott ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IMessageService
{
    /// <summary>Användarens konversationer, senast aktiva först.</summary>
    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>En konversation, eller null om den inte finns eller användaren inte är med i den.</summary>
    Task<ConversationDetails?> GetConversationAsync(int conversationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Konversationen med en viss person, eller null om de inte har skrivit till varandra.</summary>
    Task<int?> FindConversationAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);

    /// <summary>Den man vill skriva till, för sidan "New message", eller null om personen inte finns (eller är en själv).</summary>
    Task<MessageRecipient?> GetRecipientAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);

    /// <summary>Skickar ett meddelande och skapar konversationen om den inte finns.</summary>
    Task<SentMessage> SendAsync(string senderId, string recipientId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Avsändaren redigerar sitt meddelande, om det inte är dolt.</summary>
    Task EditAsync(long postId, string userId, string? content, CancellationToken cancellationToken = default);

    /// <summary>Avsändaren tar bort sitt meddelande (det ligger kvar som "This message was deleted.").</summary>
    Task DeleteAsync(long postId, string userId, CancellationToken cancellationToken = default);

    Task BlockAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);

    Task UnblockAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);

    /// <summary>De som användaren har blockerat.</summary>
    Task<IReadOnlyList<BlockedUser>> GetBlockedAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Antal konversationer med olästa meddelanden, för märket i menyn.</summary>
    Task<int> CountUnreadConversationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Om användaren får ett mejl när någon skriver (standard ja).</summary>
    Task<bool> GetEmailOnMessageAsync(string userId, CancellationToken cancellationToken = default);

    Task SetEmailOnMessageAsync(string userId, bool enabled, CancellationToken cancellationToken = default);
}

public sealed record ConversationSummary(
    int Id,
    string OtherUserId,
    string OtherName,
    string? OtherAvatarUrl,
    bool OtherIsDeleted,
    string? LastSnippet,
    bool LastIsMine,
    DateTimeOffset LastAt,
    int UnreadCount);

/// <summary>
/// En konversation sedd av en av deltagarna. <see cref="CanWrite"/> är falskt om någon av dem har blockerat den andra
/// eller den andra har tagit bort sitt konto.
/// </summary>
public sealed record ConversationDetails(
    int Id,
    int ThreadId,
    string OtherUserId,
    string OtherName,
    string? OtherAvatarUrl,
    bool OtherIsDeleted,
    bool YouBlocked,
    bool BlockedYou,
    bool CanWrite);

public sealed record MessageRecipient(string Id, string Name, string? AvatarUrl, int? ConversationId, bool YouBlocked, bool BlockedYou);

/// <summary>
/// Ett skickat meddelande. <see cref="NotifyEmail"/> är mottagarens adress om hen ska få ett mejl om det: hen vill ha
/// mejl och hade inga olästa meddelanden i konversationen innan (ett mejl per omgång, inte ett per meddelande).
/// </summary>
public sealed record SentMessage(int ConversationId, int ThreadId, long PostId, string SenderName, string RecipientName, string? NotifyEmail);

public sealed record BlockedUser(string Id, string Name, string? AvatarUrl, DateTimeOffset BlockedAt);

public static class MessageLinks
{
    public const string Messages = "messages";

    public static string Conversation(int conversationId) => $"messages/{conversationId}";

    public static string Message(int conversationId, long postId) => $"messages/{conversationId}?post={postId}#post-{postId}";

    /// <summary>Skriv till någon: öppnar den befintliga konversationen eller en ny.</summary>
    public static string To(string userId) => $"messages/to/{userId}";
}
