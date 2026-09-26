using Taleshaven.Core.Site;

namespace Taleshaven.Infrastructure.Identity;

/// <summary>Fasta id:n för sajtens roller i AspNetRoles.</summary>
internal static class SiteRoleIds
{
    public static IReadOnlyDictionary<SiteRole, string> All { get; } = new Dictionary<SiteRole, string>
    {
        [SiteRole.Administrator] = "administrator",
        [SiteRole.Manager] = "manager",
    };

    public static SiteRole? RoleOf(string roleId) =>
        All.Where(pair => pair.Value == roleId).Select(pair => (SiteRole?)pair.Key).FirstOrDefault();
}
