namespace Taleshaven.Core.Moderation;

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ReportReason
{
    Harassment = 1,
    Hate = 2,
    Spam = 3,
    Other = 4,
}

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ReportStatus
{
    Open = 0,

    /// <summary>Rapporten var obefogad (B70).</summary>
    Dismissed = 1,

    /// <summary>Inlägget doldes eller författaren fick en åtgärd (B70).</summary>
    Actioned = 2,
}

/// <summary>
/// En rapport om ett olämpligt inlägg (B70). Varje användare rapporterar samma inlägg högst en gång. Rapporten syns för
/// kampanjens GM (om inlägget inte är GM:s eget) och för sajtens moderatorer (administratörer och managers).
/// </summary>
public class PostReport
{
    private PostReport() { }

    public long Id { get; private set; }
    public long PostId { get; private set; }
    public string ReporterId { get; private set; } = "";
    public ReportReason Reason { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public ReportStatus Status { get; private set; }
    public string? ResolvedById { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static PostReport Create(long postId, string reporterId, ReportReason reason, string? comment, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reporterId);
        if (!Enum.IsDefined(reason))
            throw new CampaignRuleException("Choose a reason for the report.");
        var text = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (text?.Length > ModerationLimits.CommentMaxLength)
            throw new CampaignRuleException($"The comment can be at most {ModerationLimits.CommentMaxLength} characters.");

        return new PostReport { PostId = postId, ReporterId = reporterId, Reason = reason, Comment = text, CreatedAt = now };
    }

    public void Resolve(ReportStatus status, string moderatorId, DateTimeOffset now)
    {
        if (status == ReportStatus.Open)
            throw new ArgumentOutOfRangeException(nameof(status));
        Status = status;
        ResolvedById = moderatorId;
        ResolvedAt = now;
    }
}

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ModerationActionKind
{
    DismissReports = 1,
    HidePost = 2,
    UnhidePost = 3,
    Warn = 4,
    Suspend = 5,
    LiftSuspension = 6,
    Ban = 7,
    LiftBan = 8,
}

/// <summary>
/// En rad i moderationsloggen (B70): vem gjorde vad, när och varför. GM ser loggen för sina kampanjer, moderatorerna allt.
/// </summary>
public class ModerationAction
{
    private ModerationAction() { }

    public long Id { get; private set; }
    public ModerationActionKind Kind { get; private set; }
    public string ModeratorId { get; private set; } = "";
    public int? CampaignId { get; private set; }
    public long? PostId { get; private set; }
    public string? TargetUserId { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>Hur länge en avstängning gäller.</summary>
    public DateTimeOffset? Until { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ModerationAction Create(ModerationActionKind kind, string moderatorId, DateTimeOffset now, int? campaignId = null,
        long? postId = null, string? targetUserId = null, string? reason = null, DateTimeOffset? until = null) =>
        new()
        {
            Kind = kind,
            ModeratorId = moderatorId,
            CampaignId = campaignId,
            PostId = postId,
            TargetUserId = targetUserId,
            Reason = ModerationLimits.Reason(reason, required: false),
            Until = until,
            CreatedAt = now,
        };
}

/// <summary>
/// Ett meddelande från en moderator till en användare, t.ex. en varning (B70). Visas överst på sidan tills användaren har
/// bekräftat att hen läst det.
/// </summary>
public class UserNotice
{
    private UserNotice() { }

    public long Id { get; private set; }
    public string UserId { get; private set; } = "";
    public string Message { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public static UserNotice Create(string userId, string? message, DateTimeOffset now) =>
        new() { UserId = userId, Message = ModerationLimits.Reason(message, required: true)!, CreatedAt = now };

    public void Acknowledge(DateTimeOffset now) => AcknowledgedAt ??= now;
}

public static class ModerationLimits
{
    public const int CommentMaxLength = 500;
    public const int ReasonMaxLength = 500;

    /// <summary>Avstängningar som moderatorerna kan välja, i dagar.</summary>
    public static readonly IReadOnlyList<int> SuspensionDays = [1, 3, 7, 30];

    public static string? Reason(string? text, bool required)
    {
        var trimmed = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (required && trimmed is null)
            throw new CampaignRuleException("Write a message.");
        if (trimmed?.Length > ReasonMaxLength)
            throw new CampaignRuleException($"The text can be at most {ReasonMaxLength} characters.");
        return trimmed;
    }

    public static string ReasonText(ReportReason reason) => reason switch
    {
        ReportReason.Harassment => "Harassment",
        ReportReason.Hate => "Hate",
        ReportReason.Spam => "Spam",
        _ => "Other",
    };
}
