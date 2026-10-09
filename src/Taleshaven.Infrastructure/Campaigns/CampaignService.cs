using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Campaigns;

internal sealed class CampaignService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : ICampaignService
{
    public async Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(string viewerId, string? query = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // Sökningen görs i minnet: ord i namnet kan inte matchas på början i SQL utan fulltextsökning, och kampanjerna är få.
        var terms = TagList.ParseSearch(query);
        var campaigns = await (
                from c in db.Campaigns.AsNoTracking()
                join gm in db.Users on c.GameMasterId equals gm.Id
                orderby c.Status, c.CreatedAt descending
                select new CampaignListItem(
                    c.Id,
                    c.Name,
                    gm.DisplayName,
                    c.Tags,
                    c.Memberships.Count,
                    c.MaxPlayers,
                    c.Status,
                    c.GameMasterId == viewerId ? CampaignRole.GameMaster
                        : c.Memberships.Any(m => m.UserId == viewerId) ? CampaignRole.Player
                        : CampaignRole.None,
                    c.Applications.Any(a => a.UserId == viewerId && a.Status == ApplicationStatus.Pending)))
            .ToListAsync(cancellationToken);

        return terms.Count == 0 ? campaigns : campaigns.Where(c => Campaign.MatchesSearch(c.Name, c.Tags, terms)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetCampaignTagsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var tagLists = await db.Campaigns.AsNoTracking().Select(c => c.Tags).ToListAsync(cancellationToken);
        return tagLists
            .SelectMany(tags => tags)
            .GroupBy(tag => tag)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(limit)
            .Select(g => g.Key)
            .ToList();
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
                    c.Tags,
                    c.DefaultRoll,
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
            campaign.ViewerApplication,
            campaign.Tags,
            campaign.DefaultRoll);
    }

    public async Task<int> CreateCampaignAsync(string gameMasterId, NewCampaign campaign, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var entity = Campaign.Create(gameMasterId, campaign.Name, campaign.Description, campaign.MaxPlayers, now, campaign.Tags, campaign.DefaultRoll);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // En ny kampanj har inga trådar; GM skapar dem själv (B25).
        db.Campaigns.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateCampaignAsync(int campaignId, string userId, CampaignSettings settings, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadManagedCampaignAsync(db, campaignId, userId, cancellationToken);

        campaign.UpdateDetails(settings.Name, settings.Description, settings.MaxPlayers, settings.Tags, settings.DefaultRoll);
        campaign.ChangeStatus(settings.Status);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePlayerAsync(int campaignId, string userId, string playerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadManagedCampaignAsync(db, campaignId, userId, cancellationToken);

        campaign.RemovePlayer(playerId);
        await db.SaveChangesAsync(cancellationToken);

        // Läspositionerna tas bort, så att spelaren börjar om från nuläget om hen godkänns igen (F18).
        await db.ReadMarkers
            .Where(m => m.UserId == playerId && db.Threads.Any(t => t.Id == m.ThreadId && t.CampaignId == campaignId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteCampaignAsync(int campaignId, string userId, string? confirmationName, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadManagedCampaignAsync(db, campaignId, userId, cancellationToken);

        if (!string.Equals(confirmationName?.Trim(), campaign.Name, StringComparison.Ordinal))
            throw new CampaignRuleException("Type the campaign's name exactly to confirm that it should be deleted.");

        // Databasen raderar allt som hör till kampanjen i samma sats (kaskad): kanaler, inlägg, historik,
        // krönika, karaktärer, ansökningar, medlemskap och läspositioner. Porträtten ligger kvar i biblioteket (B19).
        await db.Campaigns.Where(c => c.Id == campaignId).ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<Campaign> LoadManagedCampaignAsync(TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns
            .Include(c => c.Memberships)
            .SingleOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("The campaign doesn't exist.");

        if (!CampaignPermissions.CanManageCampaign(campaign.IsGameMaster(userId) ? CampaignRole.GameMaster : CampaignRole.None))
            throw new CampaignRuleException("Only the campaign's GM can change the campaign.");

        return campaign;
    }
}
