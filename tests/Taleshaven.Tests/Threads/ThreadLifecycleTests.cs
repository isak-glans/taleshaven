using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

/// <summary>Trådar (B25, B30–B32, B37).</summary>
public class ThreadLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static CampaignThread Chapter() => CampaignThread.Create(7, "Chapter 1 – Arrival", 1, "gm", Now);

    [Fact]
    public void Create_TrimsAndStartsActive()
    {
        var thread = CampaignThread.Create(7, "  OOC  ", 3, "gm", Now);

        Assert.Equal("OOC", thread.Title);
        Assert.Equal(ThreadStatus.Active, thread.Status);
        Assert.Equal(3, thread.Position);
        Assert.Equal(Now, thread.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_RequiresTitle(string? title)
    {
        Assert.Throws<CampaignRuleException>(() => CampaignThread.Create(7, title, 1, "gm", Now));
    }

    [Fact]
    public void Title_HasMaxLength()
    {
        Assert.Throws<CampaignRuleException>(() =>
            CampaignThread.Create(7, new string('a', ThreadLimits.TitleMaxLength + 1), 1, "gm", Now));
        Assert.Throws<CampaignRuleException>(() => Chapter().Rename(new string('a', ThreadLimits.TitleMaxLength + 1), Now));
    }

    [Fact]
    public void Rename_ChangesTitle()
    {
        var thread = Chapter();

        thread.Rename(" Chapter 1 – The Gate ", Now.AddDays(1));

        Assert.Equal("Chapter 1 – The Gate", thread.Title);
        Assert.Equal(Now.AddDays(1), thread.UpdatedAt);
    }

    [Fact]
    public void Complete_AndReopen()
    {
        var thread = Chapter();

        thread.Complete(Now.AddDays(3));
        Assert.Equal(ThreadStatus.Completed, thread.Status);
        Assert.Throws<CampaignRuleException>(() => thread.Complete(Now));

        thread.Reopen(Now.AddDays(4));
        Assert.Equal(ThreadStatus.Active, thread.Status);
        Assert.Equal(Now.AddDays(4), thread.UpdatedAt);
        Assert.Throws<CampaignRuleException>(() => thread.Reopen(Now));
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
