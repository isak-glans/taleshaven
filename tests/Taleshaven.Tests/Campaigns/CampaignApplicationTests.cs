using Taleshaven.Core;
using Taleshaven.Core.Campaigns;

namespace Taleshaven.Tests.Campaigns;

public class CampaignApplicationTests
{
    private const string Gm = "gm";
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private static Campaign NewCampaign(int? maxPlayers = 4) => Campaign.Create(Gm, "Curse of Strahd", null, maxPlayers, Now);

    [Fact]
    public void Apply_CreatesPendingApplicationWithTrimmedMessage()
    {
        var campaign = NewCampaign();

        var application = campaign.Apply("anna", "  Hej! Jag vill spela en paladin.  ", Now);

        Assert.Equal(ApplicationStatus.Pending, application.Status);
        Assert.Equal("anna", application.UserId);
        Assert.Equal("Hej! Jag vill spela en paladin.", application.Message);
        Assert.Equal(Now, application.SubmittedAt);
        Assert.Null(application.DecidedAt);
        Assert.NotEqual(Guid.Empty, application.Id);
        Assert.Single(campaign.Applications);
    }

    [Fact]
    public void Apply_AllowsEmptyMessage()
    {
        var application = NewCampaign().Apply("anna", null, Now);

        Assert.Equal("", application.Message);
    }

    [Fact]
    public void Apply_RejectsGameMaster()
    {
        var ex = Assert.Throws<CampaignRuleException>(() => NewCampaign().Apply(Gm, null, Now));
        Assert.Contains("GM", ex.Message);
    }

    [Fact]
    public void Apply_RejectsExistingPlayer()
    {
        var campaign = NewCampaign();
        Approve(campaign, "anna");

        Assert.Throws<CampaignRuleException>(() => campaign.Apply("anna", null, Now));
    }

    [Fact]
    public void Apply_RejectsSecondPendingApplication()
    {
        var campaign = NewCampaign();
        campaign.Apply("anna", null, Now);

        Assert.Throws<CampaignRuleException>(() => campaign.Apply("anna", null, Now));
    }

    [Fact]
    public void Apply_AllowsNewApplicationAfterRejection()
    {
        var campaign = NewCampaign();
        var first = campaign.Apply("anna", null, Now);
        campaign.RejectApplication(first.Id, Gm, Now);

        var second = campaign.Apply("anna", "Andra försöket", Now.AddDays(1));

        Assert.Equal(ApplicationStatus.Pending, second.Status);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Apply_RejectsWhenCampaignIsFull()
    {
        var campaign = NewCampaign(maxPlayers: 1);
        Approve(campaign, "anna");

        Assert.Throws<CampaignRuleException>(() => campaign.Apply("bertil", null, Now));
    }

    [Fact]
    public void Apply_RejectsTooLongMessage()
    {
        var message = new string('a', CampaignLimits.ApplicationMessageMaxLength + 1);

        Assert.Throws<CampaignRuleException>(() => NewCampaign().Apply("anna", message, Now));
    }

    [Fact]
    public void Approve_AddsPlayerAndRecordsDecision()
    {
        var campaign = NewCampaign();
        var application = campaign.Apply("anna", null, Now);
        var decidedAt = Now.AddHours(2);

        campaign.ApproveApplication(application.Id, Gm, decidedAt);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Equal(Gm, application.DecidedById);
        Assert.Equal(decidedAt, application.DecidedAt);
        Assert.True(campaign.IsPlayer("anna"));
        Assert.Equal(decidedAt, campaign.Memberships.Single().JoinedAt);
    }

    [Fact]
    public void Reject_RecordsDecisionWithoutAddingPlayer()
    {
        var campaign = NewCampaign();
        var application = campaign.Apply("anna", null, Now);

        campaign.RejectApplication(application.Id, Gm, Now);

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.False(campaign.IsPlayer("anna"));
    }

    [Fact]
    public void Decisions_RequireGameMaster()
    {
        var campaign = NewCampaign();
        var application = campaign.Apply("anna", null, Now);

        Assert.Throws<CampaignRuleException>(() => campaign.ApproveApplication(application.Id, "bertil", Now));
        Assert.Throws<CampaignRuleException>(() => campaign.RejectApplication(application.Id, "anna", Now));
        Assert.Equal(ApplicationStatus.Pending, application.Status);
    }

    [Fact]
    public void Decisions_RequirePendingApplication()
    {
        var campaign = NewCampaign();
        var application = campaign.Apply("anna", null, Now);
        campaign.RejectApplication(application.Id, Gm, Now);

        Assert.Throws<CampaignRuleException>(() => campaign.ApproveApplication(application.Id, Gm, Now));
        Assert.Throws<CampaignRuleException>(() => campaign.RejectApplication(Guid.NewGuid(), Gm, Now));
    }

    [Fact]
    public void Approve_RejectsWhenCampaignIsFull()
    {
        var campaign = NewCampaign(maxPlayers: 1);
        var anna = campaign.Apply("anna", null, Now);
        var bertil = campaign.Apply("bertil", null, Now);
        campaign.ApproveApplication(anna.Id, Gm, Now);

        Assert.Throws<CampaignRuleException>(() => campaign.ApproveApplication(bertil.Id, Gm, Now));
        Assert.Equal(ApplicationStatus.Pending, bertil.Status);
        Assert.Single(campaign.Memberships);
    }

    [Fact]
    public void Details_CanViewerApply_FollowsViewerApplication()
    {
        var details = new CampaignDetails(1, "Namn", "", "GM", 4, CampaignStatus.OpenForApplications, Now, [], CampaignRole.None, null);

        Assert.True(details.CanViewerApply);
        Assert.False((details with { ViewerApplication = new(ApplicationStatus.Pending, Now, null) }).CanViewerApply);
        Assert.True((details with { ViewerApplication = new(ApplicationStatus.Rejected, Now, Now) }).CanViewerApply);
        Assert.False((details with { ViewerRole = CampaignRole.Player }).CanViewerApply);
    }

    private static void Approve(Campaign campaign, string userId)
    {
        var application = campaign.Apply(userId, null, Now);
        campaign.ApproveApplication(application.Id, Gm, Now);
    }
}
