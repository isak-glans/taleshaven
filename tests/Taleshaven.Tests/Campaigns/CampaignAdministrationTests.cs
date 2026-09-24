using Taleshaven.Core;
using Taleshaven.Core.Campaigns;

namespace Taleshaven.Tests.Campaigns;

public class CampaignAdministrationTests
{
    private const string Gm = "gm";
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static Campaign CampaignWithPlayers(params string[] players)
    {
        var campaign = Campaign.Create(Gm, "Curse of Strahd", "Gotisk skräck", 4, Now);
        foreach (var player in players)
        {
            var application = campaign.Apply(player, null, Now);
            campaign.ApproveApplication(application.Id, Gm, Now);
        }
        return campaign;
    }

    [Fact]
    public void UpdateDetails_ChangesNameDescriptionAndMaxPlayers()
    {
        var campaign = CampaignWithPlayers("anna");

        campaign.UpdateDetails("  Barovia  ", "  Nytt kapitel ", 6);

        Assert.Equal("Barovia", campaign.Name);
        Assert.Equal("Nytt kapitel", campaign.Description);
        Assert.Equal(6, campaign.MaxPlayers);
    }

    [Fact]
    public void UpdateDetails_AllowsUnlimitedPlayers()
    {
        var campaign = CampaignWithPlayers("anna");

        campaign.UpdateDetails("Barovia", null, null);

        Assert.Null(campaign.MaxPlayers);
    }

    [Fact]
    public void UpdateDetails_CannotSetMaxBelowCurrentPlayers()
    {
        var campaign = CampaignWithPlayers("anna", "bertil");

        var ex = Assert.Throws<CampaignRuleException>(() => campaign.UpdateDetails("Barovia", null, 1));
        Assert.Contains("2 spelare", ex.Message);
        Assert.Equal(4, campaign.MaxPlayers);
    }

    [Fact]
    public void UpdateDetails_ValidatesLikeCreate()
    {
        var campaign = CampaignWithPlayers();

        Assert.Throws<CampaignRuleException>(() => campaign.UpdateDetails(" ", null, 4));
        Assert.Throws<CampaignRuleException>(() => campaign.UpdateDetails("Namn", null, CampaignLimits.MaxPlayers + 1));
        Assert.Equal("Curse of Strahd", campaign.Name);
    }

    [Theory]
    [InlineData(CampaignStatus.Ongoing)]
    [InlineData(CampaignStatus.Closed)]
    [InlineData(CampaignStatus.Archived)]
    [InlineData(CampaignStatus.OpenForApplications)]
    public void ChangeStatus_AllowsEveryStatus(CampaignStatus status)
    {
        var campaign = CampaignWithPlayers();

        campaign.ChangeStatus(status);

        Assert.Equal(status, campaign.Status);
    }

    [Fact]
    public void ChangeStatus_RejectsUnknownValue()
    {
        Assert.Throws<CampaignRuleException>(() => CampaignWithPlayers().ChangeStatus((CampaignStatus)42));
    }

    [Fact]
    public void ClosingStopsApplications()
    {
        var campaign = CampaignWithPlayers();

        campaign.ChangeStatus(CampaignStatus.Ongoing);

        Assert.False(campaign.AcceptsApplications);
        Assert.Throws<CampaignRuleException>(() => campaign.Apply("anna", null, Now));
    }

    [Fact]
    public void RemovePlayer_RemovesMembershipAndFreesSeat()
    {
        var campaign = Campaign.Create(Gm, "Full kampanj", null, 1, Now);
        var application = campaign.Apply("anna", null, Now);
        campaign.ApproveApplication(application.Id, Gm, Now);
        Assert.False(campaign.AcceptsApplications);

        campaign.RemovePlayer("anna");

        Assert.False(campaign.IsPlayer("anna"));
        Assert.True(campaign.AcceptsApplications);
    }

    [Fact]
    public void RemovedPlayerCanApplyAgain()
    {
        var campaign = CampaignWithPlayers("anna");

        campaign.RemovePlayer("anna");

        Assert.Equal(ApplicationStatus.Pending, campaign.Apply("anna", null, Now).Status);
    }

    [Fact]
    public void RemovePlayer_RejectsNonPlayer()
    {
        Assert.Throws<CampaignRuleException>(() => CampaignWithPlayers("anna").RemovePlayer("bertil"));
        Assert.Throws<CampaignRuleException>(() => CampaignWithPlayers("anna").RemovePlayer(Gm));
    }

    [Theory]
    [InlineData(CampaignRole.GameMaster, true)]
    [InlineData(CampaignRole.Player, false)]
    [InlineData(CampaignRole.None, false)]
    public void OnlyGameMasterManagesCampaign(CampaignRole role, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanManageCampaign(role));
    }
}
