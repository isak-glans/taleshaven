using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Media;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Campaigns;

internal sealed class CampaignService(IDbContextFactory<TaleshavenDbContext> dbFactory, IImageStore imageStore, TimeProvider timeProvider) : ICampaignService
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

    public async Task UpdateCampaignAsync(int campaignId, string userId, CampaignSettings settings, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadManagedCampaignAsync(db, campaignId, userId, cancellationToken);

        campaign.UpdateDetails(settings.Name, settings.Description, settings.MaxPlayers);
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
            throw new CampaignRuleException("Skriv kampanjens namn exakt för att bekräfta att den ska raderas.");

        var avatarKeys = await db.Characters
            .Where(c => c.CampaignId == campaignId && c.AvatarKey != null)
            .Select(c => c.AvatarKey!)
            .ToListAsync(cancellationToken);

        // Databasen raderar allt som hör till kampanjen i samma sats (kaskad): kanaler, inlägg, historik,
        // krönika, karaktärer, ansökningar, medlemskap och läspositioner.
        await db.Campaigns.Where(c => c.Id == campaignId).ExecuteDeleteAsync(cancellationToken);

        foreach (var key in avatarKeys)
            imageStore.DeleteAvatar(key);
    }

    private static async Task<Campaign> LoadManagedCampaignAsync(TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var campaign = await db.Campaigns
            .Include(c => c.Memberships)
            .SingleOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("Kampanjen finns inte.");

        if (!CampaignPermissions.CanManageCampaign(campaign.IsGameMaster(userId) ? CampaignRole.GameMaster : CampaignRole.None))
            throw new CampaignRuleException("Endast kampanjens GM kan ändra kampanjen.");

        return campaign;
    }
}
