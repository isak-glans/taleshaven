using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Site;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Identity;

/// <param name="configuredAdminEmails">Administratörerna i konfigurationen (<c>Admin:Emails</c>).</param>
internal sealed class SiteRoleService(IDbContextFactory<TaleshavenDbContext> dbFactory, IReadOnlyList<string> configuredAdminEmails)
    : ISiteRoleService
{
    public async Task<IReadOnlySet<SiteRole>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new HashSet<SiteRole>();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await GetRolesAsync(db, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<SiteRoleHolder>> GetHoldersAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureAdminAsync(db, adminId, cancellationToken);

        var granted = await (
                from ur in db.UserRoles
                join u in db.Users on ur.UserId equals u.Id
                select new { u.Id, u.DisplayName, u.Email, ur.RoleId })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var holders = granted
            .Where(r => SiteRoleIds.RoleOf(r.RoleId) is not null)
            .Select(r => new SiteRoleHolder(r.Id, r.DisplayName, r.Email ?? "", SiteRoleIds.RoleOf(r.RoleId)!.Value, FromConfiguration: false))
            .ToList();

        // Administratörer från konfigurationen visas som sådana, även om de också har fått rollen på sajten.
        foreach (var user in await ConfiguredAdminsAsync(db, cancellationToken))
        {
            holders.RemoveAll(h => h.UserId == user.Id && h.Role == SiteRole.Administrator);
            holders.Add(new SiteRoleHolder(user.Id, user.DisplayName, user.Email ?? "", SiteRole.Administrator, FromConfiguration: true));
        }

        return holders
            .OrderBy(h => h.Role)
            .ThenBy(h => h.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task GrantAsync(string adminId, string? email, SiteRole role, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureAdminAsync(db, adminId, cancellationToken);

        var normalizedEmail = email?.Trim().ToUpperInvariant() ?? "";
        if (normalizedEmail.Length == 0)
            throw new CampaignRuleException("Ange en e-postadress.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken)
            ?? throw new CampaignRuleException("Det finns ingen användare med den e-postadressen.");

        if ((await GetRolesAsync(db, user.Id, cancellationToken)).Contains(role))
            throw new CampaignRuleException($"{user.DisplayName} är redan {SiteRoles.DisplayName(role).ToLowerInvariant()}.");

        db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = SiteRoleIds.All[role] });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(string adminId, string userId, SiteRole role, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureAdminAsync(db, adminId, cancellationToken);

        var fromConfiguration = role == SiteRole.Administrator
            && (await ConfiguredAdminsAsync(db, cancellationToken)).Any(u => u.Id == userId);
        SitePermissions.EnsureCanRevoke(adminId, userId, role, fromConfiguration);

        var roleId = SiteRoleIds.All[role];
        var grant = await db.UserRoles.SingleOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken)
            ?? throw new CampaignRuleException("Användaren har inte den rollen.");

        db.UserRoles.Remove(grant);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlySet<SiteRole>> GetRolesAsync(TaleshavenDbContext db, string userId, CancellationToken cancellationToken)
    {
        var roleIds = await db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var roles = roleIds.Select(SiteRoleIds.RoleOf).OfType<SiteRole>().ToHashSet();

        if (!roles.Contains(SiteRole.Administrator) && configuredAdminEmails.Count > 0)
        {
            var user = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.EmailConfirmed })
                .SingleOrDefaultAsync(cancellationToken);

            // Bara bekräftade adresser, så att ingen kan bli administratör genom att registrera en adress hen inte äger.
            if (user is { EmailConfirmed: true } && SiteRoles.IsConfiguredAdmin(configuredAdminEmails, user.Email))
                roles.Add(SiteRole.Administrator);
        }

        return roles;
    }

    private async Task<IReadOnlyList<ApplicationUser>> ConfiguredAdminsAsync(TaleshavenDbContext db, CancellationToken cancellationToken)
    {
        if (configuredAdminEmails.Count == 0)
            return [];

        var normalized = configuredAdminEmails.Select(e => e.Trim().ToUpperInvariant()).ToList();
        return await db.Users.AsNoTracking()
            .Where(u => u.EmailConfirmed && normalized.Contains(u.NormalizedEmail!))
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureAdminAsync(TaleshavenDbContext db, string adminId, CancellationToken cancellationToken)
    {
        if (!SitePermissions.CanManageRoles(await GetRolesAsync(db, adminId, cancellationToken)))
            throw new CampaignRuleException("Endast administratörer kan hantera roller.");
    }
}
