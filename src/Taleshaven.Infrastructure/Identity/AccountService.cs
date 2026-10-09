using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Media;
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
        var tombstoneName = DeletedAccount.TombstoneUserName(user.Id);
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
        user.PortraitId = null;
        user.About = "";

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ProfilePortrait?> GetProfilePortraitAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var portrait = await (
                from u in db.Users
                where u.Id == userId
                join p in db.Portraits on u.PortraitId equals p.Id
                select new { p.Id, p.ImageKey })
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return portrait is null ? null : new ProfilePortrait(portrait.Id, IImageStore.PortraitUrl(portrait.ImageKey));
    }

    public async Task<UserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.DisplayName,
                u.About,
                u.CreatedAt,
                AvatarKey = db.Portraits.Where(p => p.Id == u.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
                PostCount = db.Posts.Count(p => p.AuthorId == u.Id && p.DeletedAt == null),
            })
            .SingleOrDefaultAsync(cancellationToken);

        // Ett borttaget konto (B21) har ingen profil.
        if (user is null || DeletedAccount.IsTombstone(user.Id, user.UserName))
            return null;

        var campaigns = await db.Campaigns.AsNoTracking()
            .Where(c => c.GameMasterId == userId || c.Memberships.Any(m => m.UserId == userId))
            .OrderBy(c => c.Status)
            .ThenBy(c => c.Name)
            .Select(c => new ProfileCampaign(c.Id, c.Name, c.GameMasterId == userId, c.Status))
            .ToListAsync(cancellationToken);

        return new UserProfile(user.Id, user.DisplayName, user.AvatarKey is null ? null : IImageStore.PortraitUrl(user.AvatarKey),
            user.About, user.CreatedAt, user.PostCount, campaigns);
    }

    public async Task SetProfilePortraitAsync(string userId, int? portraitId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Användaren finns inte.");

        if (portraitId is { } id && !await db.Portraits.AnyAsync(p => p.Id == id, cancellationToken))
            throw new CampaignRuleException("The picture doesn't exist any more. Choose another one.");

        user.PortraitId = portraitId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
