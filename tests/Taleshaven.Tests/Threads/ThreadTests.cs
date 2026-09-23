using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

public class ThreadTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ThreadKind.Rpg, "RPG")]
    [InlineData(ThreadKind.Ooc, "OOC")]
    public void CreateChannel_CreatesOpenChannelOfKind(ThreadKind kind, string expectedTitle)
    {
        var channel = CampaignThread.CreateChannel(7, kind, "gm", Now);

        Assert.Equal(7, channel.CampaignId);
        Assert.Equal(kind, channel.Kind);
        Assert.Equal(expectedTitle, channel.Title);
        Assert.Equal(ThreadStatus.Open, channel.Status);
        Assert.Equal("gm", channel.CreatedById);
        Assert.Equal(Now, channel.CreatedAt);
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
    // Spelare får skriva i öppna kanaler i aktiva kampanjer.
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
}

public class ChatWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    // Skapar tidpunkter, nyast först: först "recent" inlägg inom en dag, sedan "old" inlägg för 30 dagar sedan.
    private static List<DateTimeOffset> Posts(int recent, int old) =>
        [
            .. Enumerable.Range(0, recent).Select(i => Now.AddMinutes(-i)),
            .. Enumerable.Range(0, old).Select(i => Now.AddDays(-30).AddMinutes(-i)),
        ];

    [Fact]
    public void ShowsAllRecentPostsWithinSevenDays()
    {
        Assert.Equal(45, ChatWindow.InitialCount(Posts(recent: 45, old: 56), Now));
    }

    [Fact]
    public void ShowsAtLeastMinimumWhenFewRecentPosts()
    {
        Assert.Equal(ChatWindow.InitialMinPosts, ChatWindow.InitialCount(Posts(recent: 3, old: 98), Now));
    }

    [Fact]
    public void ShowsMinimumWhenNothingIsRecent()
    {
        Assert.Equal(ChatWindow.InitialMinPosts, ChatWindow.InitialCount(Posts(recent: 0, old: 50), Now));
    }

    [Fact]
    public void NeverShowsMoreThanMaximum()
    {
        Assert.Equal(ChatWindow.InitialMaxPosts, ChatWindow.InitialCount(Posts(recent: 101, old: 0), Now));
    }

    [Fact]
    public void NeverShowsMoreThanExist()
    {
        Assert.Equal(5, ChatWindow.InitialCount(Posts(recent: 0, old: 5), Now));
        Assert.Equal(0, ChatWindow.InitialCount([], Now));
    }

    [Fact]
    public void SevenDayBoundaryIsInclusive()
    {
        List<DateTimeOffset> posts =
        [
            .. Enumerable.Range(0, 30).Select(i => Now.AddHours(-i)),
            Now.AddDays(-ChatWindow.InitialDays),
            Now.AddDays(-ChatWindow.InitialDays).AddSeconds(-1),
        ];

        Assert.Equal(31, ChatWindow.InitialCount(posts, Now));
    }
}
