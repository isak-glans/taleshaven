namespace Taleshaven.Core.Site;

/// <summary>
/// Roller för hela sajten (B18). Administratörer i konfigurationen (<c>Admin:Emails</c>) räknas alltid som administratörer
/// när deras e-postadress är bekräftad. Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface ISiteRoleService
{
    Task<IReadOnlySet<SiteRole>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Alla som har en roll. Bara för administratörer.</summary>
    Task<IReadOnlyList<SiteRoleHolder>> GetHoldersAsync(string adminId, CancellationToken cancellationToken = default);

    /// <summary>Ger användaren med e-postadressen <paramref name="email"/> en roll. Bara för administratörer.</summary>
    Task GrantAsync(string adminId, string? email, SiteRole role, CancellationToken cancellationToken = default);

    /// <summary>Tar bort en roll. Bara för administratörer; se <see cref="SitePermissions.EnsureCanRevoke"/>.</summary>
    Task RevokeAsync(string adminId, string userId, SiteRole role, CancellationToken cancellationToken = default);
}

/// <summary>En användare med en roll. <see cref="FromConfiguration"/> betyder att rollen kommer från <c>Admin:Emails</c>.</summary>
public sealed record SiteRoleHolder(string UserId, string DisplayName, string Email, SiteRole Role, bool FromConfiguration);
