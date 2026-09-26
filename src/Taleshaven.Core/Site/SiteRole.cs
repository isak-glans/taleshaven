namespace Taleshaven.Core.Site;

/// <summary>Roller för hela sajten, oberoende av kampanjroll (B18).</summary>
public enum SiteRole
{
    /// <summary>Får allt, även dela ut och ta bort roller.</summary>
    Administrator = 1,

    /// <summary>Sköter porträttbiblioteket.</summary>
    Manager = 2,
}

public static class SiteRoles
{
    public static IReadOnlyList<SiteRole> All { get; } = [SiteRole.Administrator, SiteRole.Manager];

    public static string DisplayName(SiteRole role) => role switch
    {
        SiteRole.Administrator => "Administratör",
        SiteRole.Manager => "Manager",
        _ => role.ToString(),
    };

    /// <summary>Om e-postadressen finns bland administratörerna i konfigurationen (<c>Admin:Emails</c>). Skiftläge och blanksteg spelar ingen roll.</summary>
    public static bool IsConfiguredAdmin(IEnumerable<string> configuredEmails, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalized = email.Trim();
        return configuredEmails.Any(e => string.Equals(e?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
    }
}

public static class SitePermissions
{
    public static bool CanManageRoles(IReadOnlySet<SiteRole> roles) => roles.Contains(SiteRole.Administrator);

    public static bool CanManagePortraits(IReadOnlySet<SiteRole> roles) =>
        roles.Contains(SiteRole.Administrator) || roles.Contains(SiteRole.Manager);

    /// <summary>
    /// Kastar om rollen inte får tas bort: en administratör kan inte ta bort sin egen administratörsroll (så att ingen låser ute
    /// sig själv), och administratörer från konfigurationen tas bort där, inte på sajten.
    /// </summary>
    public static void EnsureCanRevoke(string adminId, string userId, SiteRole role, bool fromConfiguration)
    {
        if (role == SiteRole.Administrator && fromConfiguration)
            throw new CampaignRuleException("Administratören anges i konfigurationen (Admin:Emails) och kan inte tas bort här.");
        if (role == SiteRole.Administrator && adminId == userId)
            throw new CampaignRuleException("Du kan inte ta bort din egen administratörsroll.");
    }
}
