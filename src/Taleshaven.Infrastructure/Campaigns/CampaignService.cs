using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Campaigns;

internal sealed class CampaignService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : ICampaignService
{
    public async Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await (
                from c in db.Campaigns.AsNoTracking()
                join gm in db.Users on c.GameMasterId equals gm.Id
                orderby c.Status, c.CreatedAt descending
                select new CampaignListItem(
                    c.Id,
                    c.Name,
                    gm.DisplayName,
                    c.Description,
                    c.Memberships.Count,
                    c.MaxPlayers,
                    c.Status,
                    c.GameMasterId == viewerId ? CampaignRole.GameMaster
                        : c.Memberships.Any(m => m.UserId == viewerId) ? CampaignRole.Player
                        : CampaignRole.None,
                    c.Applications.Any(a => a.UserId == viewerId && a.Status == ApplicationStatus.Pending)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CampaignDetails?> GetCampaignAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var campaign = await (
                from c in db.Campaigns.AsNoTracking()
                where c.Id == campaignId
                join gm in db.Users on c.GameMasterId equals gm.Id
                select new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.GameMasterId,
                    GameMasterName = gm.DisplayName,
                    c.MaxPlayers,
                    c.Status,
                    c.CreatedAt,
                    ViewerApplication = c.Applications
                        .Where(a => a.UserId == viewerId)
                        .OrderByDescending(a => a.SubmittedAt)
                        .Select(a => new ViewerApplication(a.Status, a.SubmittedAt, a.DecidedAt))
                        .FirstOrDefault(),
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (campaign is null)
            return null;

        var players = await (
                from m in db.CampaignMemberships.AsNoTracking()
                where m.CampaignId == campaignId
                join u in db.Users on m.UserId equals u.Id
                orderby m.JoinedAt
                select new CampaignPlayer(u.Id, u.DisplayName, m.JoinedAt))
            .ToListAsync(cancellationToken);

        var viewerRole = campaign.GameMasterId == viewerId ? CampaignRole.GameMaster
            : players.Any(p => p.UserId == viewerId) ? CampaignRole.Player
            : CampaignRole.None;

        return new CampaignDetails(
            campaign.Id,
            campaign.Name,
            campaign.Description,
            campaign.GameMasterName,
            campaign.MaxPlayers,
            campaign.Status,
            campaign.CreatedAt,
            players,
            viewerRole,
            campaign.ViewerApplication);
    }

    public async Task<int> CreateCampaignAsync(string gameMasterId, NewCampaign campaign, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var entity = Campaign.Create(gameMasterId, campaign.Name, campaign.Description, campaign.MaxPlayers, now);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Campaigns.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        // Varje kampanj har en RPG- och en OOC-kanal från start.
        db.Threads.Add(CampaignThread.CreateChannel(entity.Id, ThreadKind.Rpg, gameMasterId, now));
        db.Threads.Add(CampaignThread.CreateChannel(entity.Id, ThreadKind.Ooc, gameMasterId, now));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return entity.Id;
    }
}
