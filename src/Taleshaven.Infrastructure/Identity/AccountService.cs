using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
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
        await db.UserNotices.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);
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

        if (portraitId is { } id && !await db.Portraits.AnyAsync(p => p.Id == id && p.Kind == ImageKind.Portrait, cancellationToken))
            throw new CampaignRuleException("The picture doesn't exist any more. Choose another one.");

        user.PortraitId = portraitId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportPersonalDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("Användaren finns inte.");

        var roles = await (from ur in db.UserRoles where ur.UserId == userId join r in db.Roles on ur.RoleId equals r.Id select r.Name)
            .ToListAsync(cancellationToken);
        var portraitUrl = await db.Portraits.Where(p => p.Id == user.PortraitId).Select(p => p.ImageKey).FirstOrDefaultAsync(cancellationToken);
        var campaignNames = await db.Campaigns.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var gameMastered = await db.Campaigns.AsNoTracking()
            .Where(c => c.GameMasterId == userId)
            .Select(c => new { c.Id, c.Name, c.Description, Status = c.Status.ToString(), c.Tags, c.CreatedAt })
            .ToListAsync(cancellationToken);
        var memberships = await db.CampaignMemberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new { m.CampaignId, m.JoinedAt })
            .ToListAsync(cancellationToken);
        var applications = await db.CampaignApplications.AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.CampaignId, a.Message, Status = a.Status.ToString(), a.SubmittedAt, a.DecidedAt })
            .ToListAsync(cancellationToken);
        var characters = await db.Characters.AsNoTracking()
            .Where(c => c.OwnerId == userId)
            .OrderBy(c => c.CampaignId).ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        var threads = await db.Threads.AsNoTracking()
            .Where(t => db.Posts.Any(p => p.ThreadId == t.Id && p.AuthorId == userId) || t.CreatedById == userId)
            .Select(t => new { t.Id, t.CampaignId, t.Title, t.CreatedById, t.CreatedAt })
            .ToDictionaryAsync(t => t.Id, cancellationToken);
        var posts = await db.Posts.AsNoTracking()
            .Where(p => p.AuthorId == userId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
        var postIds = posts.Select(p => p.Id).ToList();
        var revisions = (await db.PostRevisions.AsNoTracking()
                .Where(r => postIds.Contains(r.PostId))
                .OrderBy(r => r.Id)
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.PostId);
        var characterNames = await db.Characters.AsNoTracking()
            .Where(c => db.Posts.Any(p => p.AuthorId == userId && p.CharacterId == c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        string? CampaignName(int id) => campaignNames.GetValueOrDefault(id);

        // Allt som användaren själv har skrivit eller som beskriver hen (GDPR art. 15, B70). Andras inlägg ingår inte,
        // och inga hemligheter (lösenordshash, säkerhetsstämplar) följer med.
        var export = new
        {
            ExportedAt = DateTimeOffset.UtcNow,
            Account = new
            {
                user.Id,
                user.Email,
                user.EmailConfirmed,
                user.DisplayName,
                user.About,
                user.CreatedAt,
                ProfilePicture = portraitUrl is null ? null : IImageStore.PortraitUrl(portraitUrl),
                Roles = roles,
            },
            CampaignsAsGameMaster = gameMastered,
            Memberships = memberships.Select(m => new { m.CampaignId, Campaign = CampaignName(m.CampaignId), m.JoinedAt }),
            Applications = applications.Select(a => new { a.CampaignId, Campaign = CampaignName(a.CampaignId), a.Message, a.Status, a.SubmittedAt, a.DecidedAt }),
            ThreadsCreated = threads.Values.Where(t => t.CreatedById == userId)
                .Select(t => new { t.Id, t.CampaignId, Campaign = CampaignName(t.CampaignId), t.Title, t.CreatedAt }),
            Characters = characters.Select(c => new
            {
                c.Id,
                c.CampaignId,
                Campaign = CampaignName(c.CampaignId),
                c.Name,
                c.IsNpc,
                c.RuleSystem,
                c.Sheet,
                c.SheetUrl,
                c.GmNote,
                c.Alias,
                c.IsHidden,
                c.IsArchived,
                Counters = c.Counters.Select(x => new { x.Label, x.Current, x.Max }),
                Conditions = c.Conditions.Select(x => x.Name),
                SavedRolls = c.SavedRolls.Select(x => new { x.Label, x.Notation }),
                c.CreatedAt,
                c.UpdatedAt,
            }),
            Posts = posts.Select(p => new
            {
                p.Id,
                CampaignId = threads.GetValueOrDefault(p.ThreadId)?.CampaignId,
                Campaign = threads.GetValueOrDefault(p.ThreadId) is { } thread ? CampaignName(thread.CampaignId) : null,
                p.ThreadId,
                Thread = threads.GetValueOrDefault(p.ThreadId)?.Title,
                Character = p.CharacterId is { } characterId ? characterNames.GetValueOrDefault(characterId) : p.DeletedCharacterName,
                p.Content,
                Rolls = p.Rolls.Select(r => new { r.Label, r.Notation, Mode = r.Mode.ToString(), r.Results, r.Total }),
                p.CreatedAt,
                p.EditedAt,
                p.DeletedAt,
                EarlierVersions = revisions[p.Id].Select(r => new { r.Content, r.WrittenAt, r.ReplacedAt }),
            }),
        };
        return JsonSerializer.SerializeToUtf8Bytes(export, ExportOptions);
    }

    private static readonly JsonSerializerOptions ExportOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
