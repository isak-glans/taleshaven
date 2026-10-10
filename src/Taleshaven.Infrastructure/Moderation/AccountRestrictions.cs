using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Moderation;

/// <summary>Avstängningar (B70): en avstängd användare kan läsa men inte skriva inlägg, söka, skapa kampanjer eller karaktärer.</summary>
internal static class AccountRestrictions
{
    public static async Task EnsureCanWriteAsync(TaleshavenDbContext db, string userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var until = await db.Users.Where(u => u.Id == userId).Select(u => u.SuspendedUntil).SingleOrDefaultAsync(cancellationToken);
        if (until > now)
            throw new CampaignRuleException($"Your account is suspended until {until:yyyy-MM-dd HH:mm} UTC. You can read but not post.");
    }
}
