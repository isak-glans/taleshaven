using Taleshaven.Core;
using Taleshaven.Core.Campaigns;

namespace Taleshaven.Tests.Campaigns;

public class CampaignTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsGameMasterAndOpensForApplications()
    {
        var campaign = Campaign.Create("gm-1", "  Curse of Strahd  ", " Gotisk skräck ", 4, Now);

        Assert.Equal("gm-1", campaign.GameMasterId);
        Assert.Equal("Curse of Strahd", campaign.Name);
        Assert.Equal("Gotisk skräck", campaign.Description);
        Assert.Equal(4, campaign.MaxPlayers);
        Assert.Equal(CampaignStatus.OpenForApplications, campaign.Status);
        Assert.Equal(Now, campaign.CreatedAt);
        Assert.True(campaign.IsGameMaster("gm-1"));
        Assert.False(campaign.IsPlayer("gm-1"));
    }

    [Fact]
    public void Create_AllowsMissingDescriptionAndUnlimitedPlayers()
    {
        var campaign = Campaign.Create("gm-1", "Blå Tornet", null, null, Now);

        Assert.Equal("", campaign.Description);
        Assert.Null(campaign.MaxPlayers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name)
    {
        Assert.Throws<CampaignRuleException>(() => Campaign.Create("gm-1", name, null, null, Now));
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        var name = new string('a', CampaignLimits.NameMaxLength + 1);

        Assert.Throws<CampaignRuleException>(() => Campaign.Create("gm-1", name, null, null, Now));
    }

    [Fact]
    public void Create_RejectsTooLongDescription()
    {
        var description = new string('a', CampaignLimits.DescriptionMaxLength + 1);

        Assert.Throws<CampaignRuleException>(() => Campaign.Create("gm-1", "Namn", description, null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CampaignLimits.MaxPlayers + 1)]
    public void Create_RejectsMaxPlayersOutOfRange(int maxPlayers)
    {
        Assert.Throws<CampaignRuleException>(() => Campaign.Create("gm-1", "Namn", null, maxPlayers, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_RequiresGameMaster(string gameMasterId)
    {
        Assert.Throws<ArgumentException>(() => Campaign.Create(gameMasterId, "Namn", null, null, Now));
    }

    [Theory]
    [InlineData(CampaignStatus.OpenForApplications, 4, 2, true)]
    [InlineData(CampaignStatus.OpenForApplications, 4, 4, false)]
    [InlineData(CampaignStatus.OpenForApplications, null, 100, true)]
    [InlineData(CampaignStatus.Ongoing, 4, 0, false)]
    [InlineData(CampaignStatus.Closed, null, 0, false)]
    [InlineData(CampaignStatus.Archived, null, 0, false)]
    public void CanAcceptApplications_DependsOnStatusAndFreeSeats(CampaignStatus status, int? maxPlayers, int playerCount, bool expected)
    {
        Assert.Equal(expected, Campaign.CanAcceptApplications(status, maxPlayers, playerCount));
    }

    [Fact]
    public void ListItem_AcceptsApplications_UsesSameRuleAsCampaign()
    {
        var full = new CampaignListItem(1, "Namn", "GM", "", 4, 4, CampaignStatus.OpenForApplications, CampaignRole.None, false);
        var open = full with { PlayerCount = 3 };

        Assert.False(full.AcceptsApplications);
        Assert.True(open.AcceptsApplications);
    }

    [Theory]
    [InlineData(CampaignRole.None, false, true)]
    [InlineData(CampaignRole.None, true, false)]
    [InlineData(CampaignRole.Player, false, false)]
    [InlineData(CampaignRole.GameMaster, false, false)]
    public void ListItem_CanViewerApply_OnlyForOutsidersWithoutPendingApplication(CampaignRole role, bool hasPending, bool expected)
    {
        var item = new CampaignListItem(1, "Namn", "GM", "", 0, 4, CampaignStatus.OpenForApplications, role, hasPending);

        Assert.Equal(expected, item.CanViewerApply);
    }
}
