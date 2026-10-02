using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

public class PostEditTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedRoller(int value) : IDiceRoller
    {
        public int RollDie(int sides) => value;
    }

    [Fact]
    public void Edit_ReplacesContentAndReturnsPreviousVersion()
    {
        var post = Post.Create(3, "anna", "Aldric drar svärdet", Now);
        var later = Now.AddMinutes(5);

        var revision = post.Edit("  Aldric drar sitt svärd.  ", later);

        Assert.Equal("Aldric drar sitt svärd.", post.Content);
        Assert.Equal(later, post.EditedAt);
        Assert.Equal(Now, post.CreatedAt);
        Assert.Equal("Aldric drar svärdet", revision.Content);
        Assert.Equal(Now, revision.WrittenAt);
        Assert.Equal(later, revision.ReplacedAt);
    }

    [Fact]
    public void Edit_SecondRevisionStartsAtPreviousEdit()
    {
        var post = Post.Create(3, "anna", "Version 1", Now);
        post.Edit("Version 2", Now.AddMinutes(1));

        var revision = post.Edit("Version 3", Now.AddMinutes(2));

        Assert.Equal("Version 2", revision.Content);
        Assert.Equal(Now.AddMinutes(1), revision.WrittenAt);
    }

    [Fact]
    public void NewPost_IsNotEdited()
    {
        Assert.Null(Post.Create(3, "anna", "Hej", Now).EditedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Edit_RejectsEmptyContent(string? content)
    {
        var post = Post.Create(3, "anna", "Hej", Now);

        Assert.Throws<CampaignRuleException>(() => post.Edit(content, Now));
        Assert.Equal("Hej", post.Content);
        Assert.Null(post.EditedAt);
    }

    [Fact]
    public void Edit_EnforcesMaxLength()
    {
        var post = Post.Create(3, "anna", "Hej", Now);

        Assert.Throws<CampaignRuleException>(() => post.Edit(new string('a', ThreadLimits.PostMaxLength + 1), Now));
    }

    [Fact]
    public void EditingChangesOnlyTheTextNotTheRolls()
    {
        var post = Post.Create(3, "anna", "I swing.", Now, rolls: [new RollRequest("1d20", "Attack")], roller: new FixedRoller(15));

        post.Edit("Sigrun swings her hammer and grins.", Now.AddMinutes(1));

        Assert.Equal("Sigrun swings her hammer and grins.", post.Content);
        Assert.Equal(15, post.Rolls.Single().Total);
    }

    [Fact]
    public void PostWithRollsCanBeEditedToNoText()
    {
        var post = Post.Create(3, "anna", "Initiative!", Now, rolls: [new RollRequest("1d20+2")], roller: new FixedRoller(9));

        post.Edit("", Now);

        Assert.Equal("", post.Content);
        Assert.Throws<CampaignRuleException>(() => Post.Create(3, "anna", "Hej", Now).Edit(" ", Now));
    }

    [Theory]
    [InlineData(CampaignRole.Player, "anna", "anna", CampaignStatus.Ongoing, ThreadStatus.Active, true)]     // eget inlägg
    [InlineData(CampaignRole.Player, "anna", "bertil", CampaignStatus.Ongoing, ThreadStatus.Active, false)]  // annans inlägg
    [InlineData(CampaignRole.GameMaster, "gm", "anna", CampaignStatus.Ongoing, ThreadStatus.Active, true)]   // GM redigerar allas (B30)
    [InlineData(CampaignRole.GameMaster, "gm", "gm", CampaignStatus.Archived, ThreadStatus.Active, true)]    // GM skriver även i arkiverad
    [InlineData(CampaignRole.Player, "anna", "anna", CampaignStatus.Archived, ThreadStatus.Active, false)]   // arkiverad kampanj
    [InlineData(CampaignRole.None, "anna", "anna", CampaignStatus.Ongoing, ThreadStatus.Active, false)]      // lämnat kampanjen
    public void CanEditPost(CampaignRole role, string userId, string authorId, CampaignStatus campaignStatus, ThreadStatus threadStatus, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanEditPost(role, campaignStatus, threadStatus, userId, authorId));
    }
}
