using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Campaigns;

internal sealed class CampaignService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : ICampaignService
{
    public async Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(CancellationToken cancellationToken = default)
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
                    c.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateCampaignAsync(string gameMasterId, NewCampaign campaign, CancellationToken cancellationToken = default)
    {
        var entity = Campaign.Create(gameMasterId, campaign.Name, campaign.Description, campaign.MaxPlayers, timeProvider.GetUtcNow());

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.Campaigns.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
