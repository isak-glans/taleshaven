using Taleshaven.Core;
using Taleshaven.Core.Moderation;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Moderation;

/// <summary>Rapporter, dolda inlägg och meddelanden till användare (B70).</summary>
public class ModerationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Report_TrimsCommentAndStartsOpen()
    {
        var report = PostReport.Create(7, "anna", ReportReason.Hate, "  Slur in the second paragraph  ", Now);

        Assert.Equal((7L, "anna", ReportReason.Hate), (report.PostId, report.ReporterId, report.Reason));
        Assert.Equal("Slur in the second paragraph", report.Comment);
        Assert.Equal(ReportStatus.Open, report.Status);
        Assert.Null(PostReport.Create(7, "anna", ReportReason.Spam, "   ", Now).Comment);
    }

    [Fact]
    public void Report_RejectsUnknownReasonAndLongComment()
    {
        Assert.Throws<CampaignRuleException>(() => PostReport.Create(7, "anna", (ReportReason)99, null, Now));
        Assert.Throws<CampaignRuleException>(() =>
            PostReport.Create(7, "anna", ReportReason.Other, new string('x', ModerationLimits.CommentMaxLength + 1), Now));
    }

    [Fact]
    public void Report_CanBeResolvedButNotReopened()
    {
        var report = PostReport.Create(7, "anna", ReportReason.Harassment, null, Now);

        report.Resolve(ReportStatus.Dismissed, "gm", Now);

        Assert.Equal((ReportStatus.Dismissed, "gm", Now), (report.Status, report.ResolvedById, report.ResolvedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => report.Resolve(ReportStatus.Open, "gm", Now));
    }

    [Fact]
    public void Post_CanBeHiddenWithReasonAndShownAgain()
    {
        var post = Post.Create(1, "bertil", "Something rude", Now);

        post.Hide("gm", " Insult ", Now);
        Assert.True(post.IsHidden);
        Assert.Equal(("gm", "Insult"), (post.HiddenById, post.HiddenReason));
        Assert.Equal("Something rude", post.Content);   // originalet sparas som bevis

        post.Unhide();
        Assert.False(post.IsHidden);
        Assert.Null(post.HiddenReason);
    }

    [Fact]
    public void Notice_NeedsAMessageAndIsAcknowledgedOnce()
    {
        Assert.Throws<CampaignRuleException>(() => UserNotice.Create("bertil", "  ", Now));

        var notice = UserNotice.Create("bertil", "Please keep it civil.", Now);
        notice.Acknowledge(Now);
        notice.Acknowledge(Now.AddDays(1));

        Assert.Equal(Now, notice.AcknowledgedAt);
    }

    [Fact]
    public void Action_KeepsReasonAndLimitsLength()
    {
        var action = ModerationAction.Create(ModerationActionKind.Suspend, "mod", Now, 3, 7, "bertil", " Repeated insults ", Now.AddDays(3));

        Assert.Equal(("Repeated insults", Now.AddDays(3)), (action.Reason, action.Until));
        Assert.Throws<CampaignRuleException>(() =>
            ModerationAction.Create(ModerationActionKind.Warn, "mod", Now, reason: new string('x', ModerationLimits.ReasonMaxLength + 1)));
    }
}
