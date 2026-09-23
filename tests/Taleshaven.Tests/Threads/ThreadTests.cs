using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

public class ThreadTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateRpg_TrimsAndOpensThread()
    {
        var thread = CampaignThread.CreateRpg(7, "  Kapitel 1  ", "  Ankomsten  ", "gm", Now);

        Assert.Equal(7, thread.CampaignId);
        Assert.Equal(ThreadKind.Rpg, thread.Kind);
        Assert.Equal("Kapitel 1", thread.Title);
        Assert.Equal("Ankomsten", thread.Description);
        Assert.Equal(ThreadStatus.Open, thread.Status);
        Assert.Equal("gm", thread.CreatedById);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateRpg_RequiresTitle(string? title)
    {
        Assert.Throws<CampaignRuleException>(() => CampaignThread.CreateRpg(7, title, null, "gm", Now));
    }

    [Fact]
    public void CreateRpg_RejectsTooLongTitleAndDescription()
    {
        Assert.Throws<CampaignRuleException>(() =>
            CampaignThread.CreateRpg(7, new string('a', ThreadLimits.TitleMaxLength + 1), null, "gm", Now));
        Assert.Throws<CampaignRuleException>(() =>
            CampaignThread.CreateRpg(7, "Titel", new string('a', ThreadLimits.DescriptionMaxLength + 1), "gm", Now));
    }

    [Fact]
    public void RpgThread_CanBeLockedAndUnlocked()
    {
        var thread = CampaignThread.CreateRpg(7, "Titel", null, "gm", Now);

        thread.SetLocked(true);
        Assert.Equal(ThreadStatus.Locked, thread.Status);

        thread.SetLocked(false);
        Assert.Equal(ThreadStatus.Open, thread.Status);
    }

    [Fact]
    public void OocThread_CannotBeLocked()
    {
        var thread = CampaignThread.CreateOoc(7, "gm", Now);

        Assert.Equal(ThreadKind.Ooc, thread.Kind);
        Assert.Throws<CampaignRuleException>(() => thread.SetLocked(true));
    }

    [Fact]
    public void Post_TrimsContent()
    {
        var post = Post.Create(3, "anna", "  Hej!\n\n  ", Now);

        Assert.Equal("Hej!", post.Content);
        Assert.Equal(3, post.ThreadId);
        Assert.Equal("anna", post.AuthorId);
        Assert.Equal(Now, post.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \n ")]
    public void Post_RejectsEmptyContent(string? content)
    {
        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", content, Now));
    }

    [Fact]
    public void Post_EnforcesMaxLength()
    {
        Post.Create(3, "anna", new string('a', ThreadLimits.PostMaxLength), Now);

        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", new string('a', ThreadLimits.PostMaxLength + 1), Now));
    }

    [Theory]
    // GM får alltid skriva.
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Ongoing, ThreadStatus.Open, true)]
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Ongoing, ThreadStatus.Locked, true)]
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Archived, ThreadStatus.Open, true)]
    // Spelare får skriva i öppna trådar i aktiva kampanjer.
    [InlineData(CampaignRole.Player, CampaignStatus.OpenForApplications, ThreadStatus.Open, true)]
    [InlineData(CampaignRole.Player, CampaignStatus.Ongoing, ThreadStatus.Open, true)]
    [InlineData(CampaignRole.Player, CampaignStatus.Ongoing, ThreadStatus.Locked, false)]
    [InlineData(CampaignRole.Player, CampaignStatus.Closed, ThreadStatus.Open, false)]
    [InlineData(CampaignRole.Player, CampaignStatus.Archived, ThreadStatus.Open, false)]
    // Utomstående får aldrig skriva.
    [InlineData(CampaignRole.None, CampaignStatus.Ongoing, ThreadStatus.Open, false)]
    public void CanWritePost_FollowsPermissionTable(CampaignRole role, CampaignStatus campaignStatus, ThreadStatus threadStatus, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanWritePost(role, campaignStatus, threadStatus));
    }

    [Theory]
    [InlineData(CampaignRole.GameMaster, true)]
    [InlineData(CampaignRole.Player, false)]
    [InlineData(CampaignRole.None, false)]
    public void CanManageThreads_OnlyGameMaster(CampaignRole role, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanManageThreads(role));
    }
}
