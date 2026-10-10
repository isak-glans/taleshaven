using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Moderation;
using Taleshaven.Core.Site;
using Taleshaven.Core.Threads;
using Taleshaven.Core.Users;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Moderation;

internal sealed class ModerationService(
    IDbContextFactory<TaleshavenDbContext> dbFactory,
    ISiteRoleService siteRoles,
    TimeProvider timeProvider) : IModerationService
{
    public async Task ReportPostAsync(int? campaignId, long postId, string userId, ReportReason reason, string? comment,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var post = await (
                from p in db.Posts
                join t in db.Threads on p.ThreadId equals t.Id
                where p.Id == postId && t.CampaignId == campaignId
                select p)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("The post doesn't exist.");

        // Ett privat meddelande (B73) kan bara rapporteras av den som har fått det.
        var conversation = await db.Conversations.AsNoTracking().SingleOrDefaultAsync(c => c.ThreadId == post.ThreadId, cancellationToken);
        if (conversation is not null && !conversation.Includes(userId))
            throw new CampaignRuleException("The post doesn't exist.");

        if (post.AuthorId == userId)
            throw new CampaignRuleException("You can't report your own post.");
        if (post.IsDeleted || post.IsHidden)
            throw new CampaignRuleException("The post has already been removed.");
        if (await db.PostReports.AnyAsync(r => r.PostId == postId && r.ReporterId == userId, cancellationToken))
            throw new CampaignRuleException("You have already reported this post.");

        db.PostReports.Add(PostReport.Create(postId, userId, reason, comment, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanSeeModerationAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (await IsSiteModeratorAsync(userId, cancellationToken))
            return true;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Campaigns.AnyAsync(c => c.GameMasterId == userId, cancellationToken);
    }

    public async Task<int> CountOpenAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await VisibleOpenReports(db, userId, await IsSiteModeratorAsync(userId, cancellationToken))
            .Select(r => r.PostId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<ModerationQueue> GetQueueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var isModerator = await IsSiteModeratorAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var reports = await (
                from r in VisibleOpenReports(db, userId, isModerator)
                join u in db.Users on r.ReporterId equals u.Id
                select new { r.PostId, Reporter = u.DisplayName, r.Reason, r.Comment, r.CreatedAt })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var postIds = reports.Select(r => r.PostId).Distinct().ToList();

        var posts = await (
                from p in db.Posts
                where postIds.Contains(p.Id)
                join t in db.Threads on p.ThreadId equals t.Id
                join c in db.Campaigns on t.CampaignId equals (int?)c.Id into campaigns
                from c in campaigns.DefaultIfEmpty()
                join u in db.Users on p.AuthorId equals u.Id
                join ch in db.Characters on p.CharacterId equals (int?)ch.Id into characters
                from ch in characters.DefaultIfEmpty()
                select new
                {
                    p.Id, CampaignId = (int?)c.Id, IsPrivateMessage = t.CampaignId == null && t.CategoryId == null,
                    CampaignName = c.Name ?? (t.CategoryId != null ? "Forum" : "Private message"), ThreadId = t.Id, ThreadTitle = t.Title, p.AuthorId,
                    AuthorName = u.DisplayName, AuthorUserName = u.UserName, CharacterName = ch.Name ?? p.DeletedCharacterName,
                    p.Content, p.CreatedAt, p.HiddenAt, p.HiddenReason,
                })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var items = posts
            .Select(p => new ReportedPost(
                p.Id, p.CampaignId, p.CampaignName, p.ThreadId, p.ThreadTitle, p.AuthorId,
                DeletedAccount.IsTombstone(p.AuthorId, p.AuthorUserName) ? DeletedAccount.DisplayName : p.AuthorName,
                p.CharacterName, p.Content, p.CreatedAt, p.HiddenAt is not null, p.HiddenReason,
                reports.Where(r => r.PostId == p.Id).OrderBy(r => r.CreatedAt)
                    .Select(r => new ReportView(r.Reporter, r.Reason, r.Comment, r.CreatedAt)).ToList(),
                CanSanctionAuthor: isModerator && p.AuthorId != userId && !DeletedAccount.IsTombstone(p.AuthorId, p.AuthorUserName),
                IsPrivateMessage: p.IsPrivateMessage))
            .OrderBy(p => p.Reports.Min(r => r.CreatedAt))
            .ToList();

        IReadOnlyList<RestrictedAccount> restricted = [];
        if (isModerator)
        {
            var now = timeProvider.GetUtcNow();
            restricted = (await db.Users.AsNoTracking()
                    .Where(u => u.SuspendedUntil > now || u.LockoutEnd == DateTimeOffset.MaxValue)
                    .OrderBy(u => u.DisplayName)
                    .Select(u => new { u.Id, u.DisplayName, u.SuspendedUntil, u.LockoutEnd })
                    .ToListAsync(cancellationToken))
                .Select(u => new RestrictedAccount(u.Id, u.DisplayName, u.SuspendedUntil > now ? u.SuspendedUntil : null,
                    u.LockoutEnd == DateTimeOffset.MaxValue))
                .ToList();
        }

        return new ModerationQueue(isModerator, items, restricted);
    }

    public async Task<IReadOnlyList<ModerationLogEntry>> GetLogAsync(string userId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var isModerator = await IsSiteModeratorAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var actions = db.ModerationActions.AsNoTracking();
        if (!isModerator)
            actions = actions.Where(a => a.CampaignId != null && db.Campaigns.Any(c => c.Id == a.CampaignId && c.GameMasterId == userId));

        return await (
                from a in actions
                orderby a.CreatedAt descending
                join m in db.Users on a.ModeratorId equals m.Id
                join t in db.Users on a.TargetUserId equals t.Id into targets
                from t in targets.DefaultIfEmpty()
                join c in db.Campaigns on a.CampaignId equals (int?)c.Id into campaigns
                from c in campaigns.DefaultIfEmpty()
                select new ModerationLogEntry(a.CreatedAt, m.DisplayName, a.Kind, a.CampaignId, c.Name, a.PostId, t.DisplayName, a.Reason, a.Until))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task DismissReportsAsync(long postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, campaignId) = await LoadModeratablePostAsync(db, postId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        await ResolveOpenReportsAsync(db, post.Id, ReportStatus.Dismissed, userId, now, cancellationToken);
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.DismissReports, userId, now, campaignId, post.Id, post.AuthorId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HidePostAsync(long postId, string userId, string? reason, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, campaignId) = await LoadModeratablePostAsync(db, postId, userId, cancellationToken);
        if (post.IsHidden)
            throw new CampaignRuleException("The post is already hidden.");
        var now = timeProvider.GetUtcNow();

        post.Hide(userId, reason, now);
        await ResolveOpenReportsAsync(db, post.Id, ReportStatus.Actioned, userId, now, cancellationToken);
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.HidePost, userId, now, campaignId, post.Id, post.AuthorId, reason));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnhidePostAsync(long postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, campaignId) = await LoadModeratablePostAsync(db, postId, userId, cancellationToken);
        if (!post.IsHidden)
            return;

        post.Unhide();
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.UnhidePost, userId, timeProvider.GetUtcNow(), campaignId, post.Id, post.AuthorId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task WarnAsync(string targetUserId, string userId, string? message, long? postId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await LoadSanctionTargetAsync(db, targetUserId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        db.UserNotices.Add(UserNotice.Create(targetUserId, message, now));
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.Warn, userId, now,
            await CampaignOfPostAsync(db, postId, cancellationToken), postId, targetUserId, message));
        await ResolveOpenReportsAsync(db, postId, ReportStatus.Actioned, userId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SuspendAsync(string targetUserId, string userId, int days, string? reason, long? postId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModerationLimits.SuspensionDays.Contains(days))
            throw new CampaignRuleException("Choose how long the suspension lasts.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var target = await LoadSanctionTargetAsync(db, targetUserId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var until = now.AddDays(days);

        target.SuspendedUntil = until;
        db.UserNotices.Add(UserNotice.Create(targetUserId,
            $"Your account is suspended until {until:yyyy-MM-dd HH:mm} UTC. You can read but not post." +
            (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}"), now));
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.Suspend, userId, now,
            await CampaignOfPostAsync(db, postId, cancellationToken), postId, targetUserId, reason, until));
        await ResolveOpenReportsAsync(db, postId, ReportStatus.Actioned, userId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LiftSuspensionAsync(string targetUserId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var target = await LoadSanctionTargetAsync(db, targetUserId, userId, cancellationToken);

        target.SuspendedUntil = null;
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.LiftSuspension, userId, timeProvider.GetUtcNow(), targetUserId: targetUserId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task BanAsync(string targetUserId, string userId, string? reason, long? postId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var target = await LoadSanctionTargetAsync(db, targetUserId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        // Identitys spärr: inloggning nekas, och en ny säkerhetsstämpel loggar ut den som redan är inloggad.
        target.LockoutEnabled = true;
        target.LockoutEnd = DateTimeOffset.MaxValue;
        target.SecurityStamp = Guid.NewGuid().ToString("N");
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.Ban, userId, now,
            await CampaignOfPostAsync(db, postId, cancellationToken), postId, targetUserId, reason));
        await ResolveOpenReportsAsync(db, postId, ReportStatus.Actioned, userId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LiftBanAsync(string targetUserId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var target = await LoadSanctionTargetAsync(db, targetUserId, userId, cancellationToken);

        target.LockoutEnd = null;
        db.ModerationActions.Add(ModerationAction.Create(ModerationActionKind.LiftBan, userId, timeProvider.GetUtcNow(), targetUserId: targetUserId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNoticeView>> GetNoticesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserNotices.AsNoTracking()
            .Where(n => n.UserId == userId && n.AcknowledgedAt == null)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new UserNoticeView(n.Id, n.Message, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task AcknowledgeNoticeAsync(string userId, long noticeId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var notice = await db.UserNotices.SingleOrDefaultAsync(n => n.Id == noticeId && n.UserId == userId, cancellationToken);
        notice?.Acknowledge(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsSiteModeratorAsync(string userId, CancellationToken cancellationToken) =>
        SitePermissions.CanModerate(await siteRoles.GetRolesAsync(userId, cancellationToken));

    // Öppna rapporter som användaren får hantera: allt för moderatorer, annars inlägg i användarens kampanjer som inte
    // är hens egna (GM:s egna inlägg hanteras bara av moderatorerna).
    private static IQueryable<PostReport> VisibleOpenReports(TaleshavenDbContext db, string userId, bool isModerator)
    {
        var reports = db.PostReports.Where(r => r.Status == ReportStatus.Open);
        if (isModerator)
            return reports;
        return from r in reports
               join p in db.Posts on r.PostId equals p.Id
               join t in db.Threads on p.ThreadId equals t.Id
               join c in db.Campaigns on t.CampaignId equals c.Id
               where c.GameMasterId == userId && p.AuthorId != userId
               select r;
    }

    // Forumets inlägg (B72) har ingen kampanj och hanteras bara av moderatorerna.
    private async Task<(Post Post, int? CampaignId)> LoadModeratablePostAsync(TaleshavenDbContext db, long postId, string userId,
        CancellationToken cancellationToken)
    {
        var row = await (
                from p in db.Posts
                where p.Id == postId
                join t in db.Threads on p.ThreadId equals t.Id
                join c in db.Campaigns on t.CampaignId equals (int?)c.Id into campaigns
                from c in campaigns.DefaultIfEmpty()
                select new { Post = p, CampaignId = (int?)c.Id, c.GameMasterId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("The post doesn't exist.");

        var isGameMaster = row.GameMasterId == userId && row.Post.AuthorId != userId;
        if (!isGameMaster && !await IsSiteModeratorAsync(userId, cancellationToken))
            throw new CampaignRuleException("You can't moderate this post.");
        return (row.Post, row.CampaignId);
    }

    // Varningar, avstängningar och spärrar är bara för sajtens moderatorer, och aldrig mot sig själv eller en administratör.
    private async Task<Identity.ApplicationUser> LoadSanctionTargetAsync(TaleshavenDbContext db, string targetUserId, string userId,
        CancellationToken cancellationToken)
    {
        if (!await IsSiteModeratorAsync(userId, cancellationToken))
            throw new CampaignRuleException("Only the site's moderators can do that.");
        if (targetUserId == userId)
            throw new CampaignRuleException("You can't do that to your own account.");
        if ((await siteRoles.GetRolesAsync(targetUserId, cancellationToken)).Contains(SiteRole.Administrator))
            throw new CampaignRuleException("Administrators can't be warned, suspended or banned.");

        var target = await db.Users.SingleOrDefaultAsync(u => u.Id == targetUserId, cancellationToken)
            ?? throw new CampaignRuleException("The user doesn't exist.");
        if (DeletedAccount.IsTombstone(target.Id, target.UserName))
            throw new CampaignRuleException("The account has been deleted.");
        return target;
    }

    private static async Task<int?> CampaignOfPostAsync(TaleshavenDbContext db, long? postId, CancellationToken cancellationToken) =>
        postId is null ? null : await (
                from p in db.Posts
                where p.Id == postId
                join t in db.Threads on p.ThreadId equals t.Id
                select (int?)t.CampaignId)
            .SingleOrDefaultAsync(cancellationToken);

    private static async Task ResolveOpenReportsAsync(TaleshavenDbContext db, long? postId, ReportStatus status, string userId,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (postId is null)
            return;
        var open = await db.PostReports.Where(r => r.PostId == postId && r.Status == ReportStatus.Open).ToListAsync(cancellationToken);
        foreach (var report in open)
            report.Resolve(status, userId, now);
    }
}
