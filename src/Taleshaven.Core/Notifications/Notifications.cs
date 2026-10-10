namespace Taleshaven.Core.Notifications;

/// <summary>
/// Klockan i toppraden (B75): det som väntar på användaren, räknat ur det som redan finns. Inga egna notiser sparas;
/// en notis försvinner när man har läst eller hanterat det den gäller.
/// </summary>
public interface INotificationService
{
    Task<NotificationSummary> GetAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="UnreadPosts"/>: olästa inlägg per kampanj där användaren är med. <see cref="PendingApplications"/>: ansökningar
/// till kampanjer där användaren är GM. <see cref="OpenReports"/>: rapporter användaren får hantera (moderatorer och GM).
/// </summary>
public sealed record NotificationSummary(
    IReadOnlyList<CampaignCount> UnreadPosts,
    int UnreadConversations,
    IReadOnlyList<CampaignCount> PendingApplications,
    int OpenReports)
{
    public static NotificationSummary Empty { get; } = new([], 0, [], 0);

    /// <summary>Antalet saker att visa på klockan: varje kampanj, konversation, ansökan och rapport.</summary>
    public int Total => UnreadPosts.Sum(c => c.Count) + UnreadConversations + PendingApplications.Sum(c => c.Count) + OpenReports;
}

public sealed record CampaignCount(int CampaignId, string CampaignName, int Count);
