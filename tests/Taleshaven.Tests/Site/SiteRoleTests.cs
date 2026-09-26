using Taleshaven.Core;
using Taleshaven.Core.Site;

namespace Taleshaven.Tests.Site;

public class SiteRoleTests
{
    [Theory]
    [InlineData("anna@exempel.se", true)]
    [InlineData("  ANNA@Exempel.SE ", true)]
    [InlineData("bertil@exempel.se", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsConfiguredAdmin_MatchesEmailRegardlessOfCase(string? email, bool expected)
    {
        Assert.Equal(expected, SiteRoles.IsConfiguredAdmin(["anna@exempel.se", " cecilia@exempel.se "], email));
    }

    [Fact]
    public void IsConfiguredAdmin_FalseWithoutConfiguration()
    {
        Assert.False(SiteRoles.IsConfiguredAdmin([], "anna@exempel.se"));
    }

    [Fact]
    public void Administrator_CanDoEverything_ManagerOnlyPortraits()
    {
        var admin = new HashSet<SiteRole> { SiteRole.Administrator };
        var manager = new HashSet<SiteRole> { SiteRole.Manager };
        var none = new HashSet<SiteRole>();

        Assert.True(SitePermissions.CanManageRoles(admin));
        Assert.True(SitePermissions.CanManagePortraits(admin));
        Assert.False(SitePermissions.CanManageRoles(manager));
        Assert.True(SitePermissions.CanManagePortraits(manager));
        Assert.False(SitePermissions.CanManageRoles(none));
        Assert.False(SitePermissions.CanManagePortraits(none));
    }

    [Fact]
    public void EnsureCanRevoke_AdminCannotRemoveOwnAdministratorRole()
    {
        Assert.Throws<CampaignRuleException>(() => SitePermissions.EnsureCanRevoke("anna", "anna", SiteRole.Administrator, fromConfiguration: false));

        // Den egna managerrollen och andras administratörsroll går bra.
        SitePermissions.EnsureCanRevoke("anna", "anna", SiteRole.Manager, fromConfiguration: false);
        SitePermissions.EnsureCanRevoke("anna", "bertil", SiteRole.Administrator, fromConfiguration: false);
    }

    [Fact]
    public void EnsureCanRevoke_ConfiguredAdministratorIsRemovedInConfiguration()
    {
        Assert.Throws<CampaignRuleException>(() => SitePermissions.EnsureCanRevoke("anna", "bertil", SiteRole.Administrator, fromConfiguration: true));
    }
}
