using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

public class ThreadTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

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

        // B23: 5 000 tecken, men databasen rymmer äldre inlägg på upp till 10 000.
        Assert.Equal(5_000, ThreadLimits.PostMaxLength);
        Assert.True(ThreadLimits.PostStorageMaxLength >= 10_000);
    }

    [Theory]
    // GM får alltid skriva.
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Ongoing, ThreadStatus.Active, true)]
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Ongoing, ThreadStatus.Completed, true)]
    [InlineData(CampaignRole.GameMaster, CampaignStatus.Archived, ThreadStatus.Active, true)]
    // Spelare får skriva i öppna kanaler i aktiva kampanjer.
    [InlineData(CampaignRole.Player, CampaignStatus.OpenForApplications, ThreadStatus.Active, true)]
    [InlineData(CampaignRole.Player, CampaignStatus.Ongoing, ThreadStatus.Active, true)]
    [InlineData(CampaignRole.Player, CampaignStatus.Ongoing, ThreadStatus.Completed, false)]
    [InlineData(CampaignRole.Player, CampaignStatus.Closed, ThreadStatus.Active, false)]
    [InlineData(CampaignRole.Player, CampaignStatus.Archived, ThreadStatus.Active, false)]
    // Utomstående får aldrig skriva.
    [InlineData(CampaignRole.None, CampaignStatus.Ongoing, ThreadStatus.Active, false)]
    public void CanWritePost_FollowsPermissionTable(CampaignRole role, CampaignStatus campaignStatus, ThreadStatus threadStatus, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanWritePost(role, campaignStatus, threadStatus));
    }
}

public class UnreadDisplayTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(99, "99")]
    [InlineData(100, "99+")]
    [InlineData(5000, "99+")]
    public void UnreadCountIsCappedForDisplay(int count, string expected)
    {
        Assert.Equal(expected, UnreadDisplay.Format(count));
    }
}