using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Messages;
using Taleshaven.Core.Moderation;
using Taleshaven.Core.Notifications;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Notifications;

internal sealed class NotificationService(
    IDbContextFactory<TaleshavenDbContext> dbFactory,
    IUnreadService unread,
    IMessageService messages,
    IModerationService moderation) : INotificationService
{
    public async Task<NotificationSummary> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (userId.Length == 0)
            return NotificationSummary.Empty;

        var unreadTotals = await unread.GetUnreadTotalsAsync(userId, cancellationToken);
        var unreadConversations = await messages.CountUnreadConversationsAsync(userId, cancellationToken);
        var openReports = await moderation.CanSeeModerationAsync(userId, cancellationToken)
            ? await moderation.CountOpenAsync(userId, cancellationToken)
            : 0;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var pending = await (
                from a in db.CampaignApplications
                where a.Status == ApplicationStatus.Pending
                join c in db.Campaigns on a.CampaignId equals c.Id
                where c.GameMasterId == userId
                group a by new { c.Id, c.Name } into g
                select new { g.Key.Id, g.Key.Name, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var campaignIds = unreadTotals.Keys.ToList();
        var names = await db.Campaigns.AsNoTracking()
            .Where(c => campaignIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return new NotificationSummary(
            unreadTotals
                .Where(u => names.ContainsKey(u.Key))
                .Select(u => new CampaignCount(u.Key, names[u.Key], u.Value))
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.CampaignName)
                .ToList(),
            unreadConversations,
            pending.Select(p => new CampaignCount(p.Id, p.Name, p.Count)).OrderBy(p => p.CampaignName).ToList(),
            openReports);
    }
}
