namespace Taleshaven.Core.Moderation;

/// <summary>
/// Rapporter och moderering (B70). Kampanjens GM modererar sitt bord: rapporter på inlägg i kampanjen (utom GM:s egna),
/// avfärda och dölja. Sajtens moderatorer (administratörer och managers) gör samma sak överallt och kan dessutom varna,
/// stänga av och spärra konton. Alla åtgärder loggas. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface IModerationService
{
    /// <summary>Rapporterar ett inlägg; <paramref name="campaignId"/> är null för forumet (B72). Det egna, ett borttaget eller ett redan dolt inlägg går inte att rapportera.</summary>
    Task ReportPostAsync(int? campaignId, long postId, string userId, ReportReason reason, string? comment,
        CancellationToken cancellationToken = default);

    /// <summary>Om användaren får se moderationssidan: sajtens moderatorer och den som är GM för någon kampanj.</summary>
    Task<bool> CanSeeModerationAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Antalet inlägg med öppna rapporter som användaren får hantera (märket i menyn).</summary>
    Task<int> CountOpenAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Inläggen med öppna rapporter som användaren får hantera, äldsta rapporten först.</summary>
    Task<ModerationQueue> GetQueueAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Loggen: GM ser åtgärder i sina kampanjer, moderatorerna allt. Nyast först.</summary>
    Task<IReadOnlyList<ModerationLogEntry>> GetLogAsync(string userId, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>Rapporterna var obefogade.</summary>
    Task DismissReportsAsync(long postId, string userId, CancellationToken cancellationToken = default);

    Task HidePostAsync(long postId, string userId, string? reason, CancellationToken cancellationToken = default);

    Task UnhidePostAsync(long postId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Ett meddelande till användaren som visas tills hen har läst det. Bara moderatorer.</summary>
    Task WarnAsync(string targetUserId, string userId, string? message, long? postId = null, CancellationToken cancellationToken = default);

    /// <summary>Avstängning i <paramref name="days"/> dagar: kan läsa men inte skriva. Bara moderatorer.</summary>
    Task SuspendAsync(string targetUserId, string userId, int days, string? reason, long? postId = null, CancellationToken cancellationToken = default);

    Task LiftSuspensionAsync(string targetUserId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Spärr: kontot kan inte logga in, och den som är inloggad loggas ut. Bara moderatorer.</summary>
    Task BanAsync(string targetUserId, string userId, string? reason, long? postId = null, CancellationToken cancellationToken = default);

    Task LiftBanAsync(string targetUserId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Olästa meddelanden till användaren, t.ex. varningar.</summary>
    Task<IReadOnlyList<UserNoticeView>> GetNoticesAsync(string userId, CancellationToken cancellationToken = default);

    Task AcknowledgeNoticeAsync(string userId, long noticeId, CancellationToken cancellationToken = default);
}

/// <summary>Moderationssidan. <see cref="Restricted"/> (avstängda och spärrade konton) fylls bara i för sajtens moderatorer.</summary>
public sealed record ModerationQueue(bool IsSiteModerator, IReadOnlyList<ReportedPost> Posts, IReadOnlyList<RestrictedAccount> Restricted);

/// <summary>
/// Ett rapporterat inlägg med sina öppna rapporter. <see cref="CanSanctionAuthor"/>: varna, stänga av och spärra.
/// <see cref="IsPrivateMessage"/>: ett privat meddelande (B73); moderatorerna ser bara det rapporterade meddelandet,
/// inte konversationen.
/// </summary>
public sealed record ReportedPost(
    long PostId,
    int? CampaignId,
    string CampaignName,
    int ThreadId,
    string ThreadTitle,
    string AuthorId,
    string AuthorName,
    string? CharacterName,
    string Content,
    DateTimeOffset CreatedAt,
    bool IsHidden,
    string? HiddenReason,
    IReadOnlyList<ReportView> Reports,
    bool CanSanctionAuthor,
    bool IsPrivateMessage = false);

public sealed record ReportView(string ReporterName, ReportReason Reason, string? Comment, DateTimeOffset CreatedAt);

public sealed record ModerationLogEntry(
    DateTimeOffset CreatedAt,
    string ModeratorName,
    ModerationActionKind Kind,
    int? CampaignId,
    string? CampaignName,
    long? PostId,
    string? TargetName,
    string? Reason,
    DateTimeOffset? Until);

public sealed record RestrictedAccount(string UserId, string DisplayName, DateTimeOffset? SuspendedUntil, bool IsBanned);

public sealed record UserNoticeView(long Id, string Message, DateTimeOffset CreatedAt);
