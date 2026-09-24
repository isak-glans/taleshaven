using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Campaigns;

/// <summary>Användarens roll i en kampanj och kampanjens status, som underlag för <see cref="CampaignPermissions"/>.</summary>
internal static class CampaignAccess
{
    public static async Task<(CampaignRole Role, CampaignStatus CampaignStatus)> GetAsync(
        TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var access = await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId)
            .Select(c => new
            {
                c.Status,
                Role = c.GameMasterId == userId ? CampaignRole.GameMaster
                    : c.Memberships.Any(m => m.UserId == userId) ? CampaignRole.Player
                    : CampaignRole.None,
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("Kampanjen finns inte.");

        return (access.Role, access.Status);
    }
}
