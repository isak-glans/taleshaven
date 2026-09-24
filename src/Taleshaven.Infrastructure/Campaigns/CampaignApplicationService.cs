using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure.Campaigns;

internal sealed class CampaignApplicationService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : ICampaignApplicationService
{
    public async Task ApplyAsync(int campaignId, string userId, string? message, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadCampaignAsync(db, campaignId, cancellationToken);

        campaign.Apply(userId, message, timeProvider.GetUtcNow());

        await SaveAsync(db, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingApplication>> GetPendingApplicationsAsync(int campaignId, string gameMasterId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await (
                from a in db.CampaignApplications.AsNoTracking()
                where a.CampaignId == campaignId
                    && a.Status == ApplicationStatus.Pending
                    && db.Campaigns.Any(c => c.Id == campaignId && c.GameMasterId == gameMasterId)
                join u in db.Users on a.UserId equals u.Id
                orderby a.SubmittedAt
                select new PendingApplication(a.Id, u.DisplayName, a.Message, a.SubmittedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task ApproveAsync(int campaignId, Guid applicationId, string gameMasterId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadCampaignAsync(db, campaignId, cancellationToken);

        var now = timeProvider.GetUtcNow();
        campaign.ApproveApplication(applicationId, gameMasterId, now);

        await SaveAsync(db, cancellationToken);

        // Den nya spelaren börjar läsa härifrån; historiken före godkännandet räknas inte som oläst.
        var applicantId = campaign.Applications.Single(a => a.Id == applicationId).UserId;
        await ReadMarkers.MarkAllReadAsync(db, campaignId, applicantId, now, cancellationToken);
    }

    public async Task RejectAsync(int campaignId, Guid applicationId, string gameMasterId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var campaign = await LoadCampaignAsync(db, campaignId, cancellationToken);

        campaign.RejectApplication(applicationId, gameMasterId, timeProvider.GetUtcNow());

        await SaveAsync(db, cancellationToken);
    }

    private static async Task<Campaign> LoadCampaignAsync(TaleshavenDbContext db, int campaignId, CancellationToken cancellationToken) =>
        await db.Campaigns
            .Include(c => c.Memberships)
            .Include(c => c.Applications.Where(a => a.Status == ApplicationStatus.Pending))
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
        ?? throw new CampaignRuleException("Kampanjen finns inte.");

    private static async Task SaveAsync(TaleshavenDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Två samtidiga anrop, t.ex. en dubbelklickad knapp.
            throw new CampaignRuleException("Ansökan har redan skickats eller behandlats. Ladda om sidan.");
        }
    }
}
