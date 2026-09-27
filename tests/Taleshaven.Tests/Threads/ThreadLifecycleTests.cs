using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

/// <summary>Trådar i fas 6 (B25, B30–B33).</summary>
public class ThreadLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static CampaignThread Story() =>
        CampaignThread.Create(7, ThreadKind.Story, "Chapter 1 – Arrival", "The fog rolls in.", 1, "gm", Now);

    [Fact]
    public void Create_TrimsAndStartsActive()
    {
        var thread = CampaignThread.Create(7, ThreadKind.Discussion, "  OOC  ", "  Planning and rules.  ", 3, "gm", Now);

        Assert.Equal(ThreadKind.Discussion, thread.Kind);
        Assert.Equal("OOC", thread.Title);
        Assert.Equal("Planning and rules.", thread.Introduction);
        Assert.Equal(ThreadStatus.Active, thread.Status);
        Assert.Equal(3, thread.Position);
        Assert.Null(thread.Chronicle);
        Assert.Equal(Now, thread.UpdatedAt);
    }

    [Fact]
    public void Create_AllowsEmptyIntroduction()
    {
        Assert.Equal("", CampaignThread.Create(7, ThreadKind.Story, "Chapter 2", null, 2, "gm", Now).Introduction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_RequiresTitle(string? title)
    {
        Assert.Throws<CampaignRuleException>(() => CampaignThread.Create(7, ThreadKind.Story, title, null, 1, "gm", Now));
    }

    [Fact]
    public void Create_EnforcesLengths()
    {
        Assert.Throws<CampaignRuleException>(() =>
            CampaignThread.Create(7, ThreadKind.Story, new string('a', ThreadLimits.TitleMaxLength + 1), null, 1, "gm", Now));
        Assert.Throws<CampaignRuleException>(() =>
            CampaignThread.Create(7, ThreadKind.Story, "Title", new string('a', ThreadLimits.IntroductionMaxLength + 1), 1, "gm", Now));
    }

    [Fact]
    public void Complete_WithChronicleStoresItAndEditor()
    {
        var thread = Story();

        thread.Complete("  The party arrived in Harrowmere.  ", "gm", Now.AddDays(3));

        Assert.Equal(ThreadStatus.Completed, thread.Status);
        Assert.Equal("The party arrived in Harrowmere.", thread.Chronicle);
        Assert.Equal("gm", thread.ChronicleEditedById);
        Assert.Equal(Now.AddDays(3), thread.ChronicleEditedAt);
        Assert.Equal("The fog rolls in.", thread.Introduction); // introduktionen ligger kvar (B33)
    }

    [Fact]
    public void Complete_WithoutChronicleLetsItBeWrittenLater()
    {
        var thread = Story();

        thread.Complete(null, "gm", Now);
        Assert.Null(thread.Chronicle);

        thread.SetChronicle("Written afterwards.", "gm", Now.AddDays(1));
        Assert.Equal("Written afterwards.", thread.Chronicle);
    }

    [Fact]
    public void Reopen_KeepsChronicle()
    {
        var thread = Story();
        thread.Complete("Summary.", "gm", Now);

        thread.Reopen(Now.AddHours(1));

        Assert.Equal(ThreadStatus.Active, thread.Status);
        Assert.Equal("Summary.", thread.Chronicle);
        Assert.Throws<CampaignRuleException>(() => thread.Reopen(Now));
    }

    [Fact]
    public void Complete_TwiceIsRejected()
    {
        var thread = Story();
        thread.Complete(null, "gm", Now);

        Assert.Throws<CampaignRuleException>(() => thread.Complete(null, "gm", Now));
    }

    [Fact]
    public void DiscussionThreads_HaveNoChronicleButCanBeCompleted()
    {
        var thread = CampaignThread.Create(7, ThreadKind.Discussion, "OOC", null, 1, "gm", Now);

        Assert.Throws<CampaignRuleException>(() => thread.SetChronicle("Nope.", "gm", Now));
        Assert.Throws<CampaignRuleException>(() => thread.Complete("Nope.", "gm", Now));
        Assert.Equal(ThreadStatus.Active, thread.Status);

        thread.Complete(null, "gm", Now);
        Assert.Equal(ThreadStatus.Completed, thread.Status);
    }

    [Fact]
    public void Chronicle_HasMaxLengthAndEmptyTextRemovesIt()
    {
        var thread = Story();

        Assert.Throws<CampaignRuleException>(() =>
            thread.SetChronicle(new string('a', ThreadLimits.ChronicleMaxLength + 1), "gm", Now));

        thread.SetChronicle("Summary.", "gm", Now);
        thread.SetChronicle("   ", "gm", Now);
        Assert.Null(thread.Chronicle);
    }

    [Fact]
    public void Post_CanReplyToAnotherPost()
    {
        Assert.Equal(41, Post.Create(3, "anna", "Agreed!", Now, replyToPostId: 41).ReplyToPostId);
    }

    [Fact]
    public void Delete_IsSoftAndKeepsContentAsHistory()
    {
        var post = Post.Create(3, "anna", "Sigrun draws her hammer.", Now);

        post.Delete("anna", Now.AddMinutes(5));

        Assert.True(post.IsDeleted);
        Assert.Equal("anna", post.DeletedById);
        Assert.Equal(Now.AddMinutes(5), post.DeletedAt);
        Assert.Equal("Sigrun draws her hammer.", post.Content);
        Assert.Throws<CampaignRuleException>(() => post.Delete("anna", Now));
        Assert.Throws<CampaignRuleException>(() => post.Edit("Changed.", Now));
    }

    [Fact]
    public void PostsWithRolls_AreMarked()
    {
        Assert.True(Post.Create(3, "anna", "[dice]1d20[/dice]", Now, roller: new OneRoller()).HasRolls);
        Assert.False(Post.Create(3, "anna", "No dice.", Now).HasRolls);
    }

    [Theory]
    [InlineData(CampaignRole.Player, "anna", "anna", ThreadStatus.Active, false, true)]     // eget inlägg
    [InlineData(CampaignRole.Player, "anna", "anna", ThreadStatus.Active, true, false)]     // eget inlägg med slag (B31)
    [InlineData(CampaignRole.Player, "anna", "bertil", ThreadStatus.Active, false, false)]  // annans inlägg
    [InlineData(CampaignRole.Player, "anna", "anna", ThreadStatus.Completed, false, false)] // avslutad tråd (B32)
    [InlineData(CampaignRole.GameMaster, "gm", "anna", ThreadStatus.Active, true, true)]    // GM tar bort allt (B30, B31)
    [InlineData(CampaignRole.GameMaster, "gm", "anna", ThreadStatus.Completed, true, true)]
    [InlineData(CampaignRole.None, "anna", "anna", ThreadStatus.Active, false, false)]      // lämnat kampanjen
    public void CanDeletePost(CampaignRole role, string userId, string authorId, ThreadStatus threadStatus, bool hasRolls, bool expected)
    {
        Assert.Equal(expected,
            CampaignPermissions.CanDeletePost(role, CampaignStatus.Ongoing, threadStatus, userId, authorId, hasRolls));
    }

    [Theory]
    [InlineData(CampaignRole.GameMaster, true)]
    [InlineData(CampaignRole.Player, false)]
    [InlineData(CampaignRole.None, false)]
    public void OnlyGameMasterManagesThreads(CampaignRole role, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanManageThreads(role));
    }

    private sealed class OneRoller : IDiceRoller
    {
        public int RollDie(int sides) => 1;
    }
}
