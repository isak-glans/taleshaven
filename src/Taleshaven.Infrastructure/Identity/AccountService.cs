using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Users;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Identity;

/// <summary>
/// Tar bort ett konto genom att anonymisera det (B21). Användarraden ligger kvar som en tom gravsten, så att inlägg,
/// karaktärer och krönikekapitel behåller sin författare; allt som pekar ut personen raderas.
/// </summary>
internal sealed class AccountService(IDbContextFactory<TaleshavenDbContext> dbFactory) : IAccountService
{
    public async Task DeleteAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Användaren finns inte.");

        DeletedAccount.EnsureCanDelete(await db.Campaigns.CountAsync(c => c.GameMasterId == userId, cancellationToken));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.CampaignMemberships.Where(m => m.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.CampaignApplications.Where(a => a.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.ReadMarkers.Where(m => m.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserClaims.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserLogins.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserPasskeys.Where(p => p.UserId == userId).ExecuteDeleteAsync(cancellationToken);

        // Användarnamnet måste vara unikt, så gravstenen får ett eget som inte kan användas för att logga in.
        var tombstoneName = $"deleted-{user.Id}";
        user.DisplayName = DeletedAccount.DisplayName;
        user.UserName = tombstoneName;
        user.NormalizedUserName = tombstoneName.ToUpperInvariant();
        user.Email = null;
        user.NormalizedEmail = null;
        user.EmailConfirmed = false;
        user.PasswordHash = null;
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.TwoFactorEnabled = false;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
