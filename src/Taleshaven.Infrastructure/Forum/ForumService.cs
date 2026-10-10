using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Forum;
using Taleshaven.Core.Media;
using Taleshaven.Core.Site;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Moderation;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure.Forum;

internal sealed class ForumService(
    IDbContextFactory<TaleshavenDbContext> dbFactory,
    ISiteRoleService siteRoles,
    TimeProvider timeProvider) : IForumService
{
    public async Task<ForumOverview> GetForumAsync(string viewerId, CancellationToken cancellationToken = default)
    {
        var canManage = await CanManageAsync(viewerId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var categories = await db.ForumCategories.AsNoTracking().OrderBy(c => c.Position).ToListAsync(cancellationToken);
        var threads = await (
                from t in db.Threads.AsNoTracking()
                where t.CategoryId != null
                let latest = db.Posts.Where(p => p.ThreadId == t.Id && p.DeletedAt == null).OrderByDescending(p => p.Id).FirstOrDefault()
                select new
                {
                    t.Id,
                    CategoryId = t.CategoryId!.Value,
                    t.Title,
                    t.IsPinned,
                    t.IsLocked,
                    PostCount = db.Posts.Count(p => p.ThreadId == t.Id && p.DeletedAt == null),
                    LatestAuthorId = latest == null ? null : latest.AuthorId,
                    LatestAt = latest == null ? (DateTimeOffset?)null : latest.CreatedAt,
                })
            .ToListAsync(cancellationToken);

        var authorIds = threads.Select(t => t.LatestAuthorId).OfType<string>().Distinct().ToList();
        var authors = await db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.DisplayName,
                u.UserName,
                AvatarKey = db.Portraits.Where(p => p.Id == u.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
            })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var unread = viewerId.Length == 0
            ? new Dictionary<int, int>()
            : (await UnreadQueries.CountPerThread(db, viewerId, db.Threads.Where(t => t.CategoryId != null)).ToListAsync(cancellationToken))
                .ToDictionary(r => r.ThreadId, r => r.Count);

        var views = categories.Select(c => new ForumCategoryView(c.Id, c.Name, c.Description, threads
                .Where(t => t.CategoryId == c.Id)
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.LatestAt)
                .Select(t =>
                {
                    var author = t.LatestAuthorId is { } id ? authors.GetValueOrDefault(id) : null;
                    var deleted = author is not null && Core.Users.DeletedAccount.IsTombstone(author.Id, author.UserName);
                    return new ForumThreadSummary(t.Id, t.Title, t.IsPinned, t.IsLocked, t.PostCount, unread.GetValueOrDefault(t.Id),
                        author is null ? null : deleted ? Core.Users.DeletedAccount.DisplayName : author.DisplayName,
                        author?.AvatarKey is { } key && !deleted ? IImageStore.PortraitUrl(key) : null,
                        t.LatestAt);
                })
                .ToList()))
            .ToList();

        return new ForumOverview(views, canManage);
    }

    public async Task<ForumThreadDetails?> GetThreadAsync(int threadId, string viewerId, CancellationToken cancellationToken = default)
    {
        var canManage = await CanManageAsync(viewerId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var row = await (
                from t in db.Threads.AsNoTracking()
                where t.Id == threadId && t.CategoryId != null
                join c in db.ForumCategories on t.CategoryId equals c.Id
                select new { Thread = t, CategoryName = c.Name, PostCount = db.Posts.Count(p => p.ThreadId == t.Id) })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var thread = row.Thread;
        return new ForumThreadDetails(thread.Id, thread.CategoryId!.Value, row.CategoryName, thread.Title, thread.IsPinned, thread.IsLocked,
            row.PostCount, CanReply: viewerId.Length > 0 && (!thread.IsLocked || canManage), canManage);
    }

    public async Task<int> CreateThreadAsync(int categoryId, string userId, string? title, string? content, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        await AccountRestrictions.EnsureCanWriteAsync(db, userId, now, cancellationToken);
        if (!await db.ForumCategories.AnyAsync(c => c.Id == categoryId, cancellationToken))
            throw new CampaignRuleException("The category doesn't exist.");

        // Tråden och det första inlägget sparas tillsammans; ett ogiltigt inlägg ska inte lämna en tom tråd.
        var thread = CampaignThread.CreateForumThread(categoryId, title, userId, now);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Threads.Add(thread);
        await db.SaveChangesAsync(cancellationToken);
        db.Posts.Add(Post.Create(thread.Id, userId, content, now));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return thread.Id;
    }

    public async Task<long> ReplyAsync(int threadId, string userId, string? content, long? replyToPostId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var thread = await LoadThreadAsync(db, threadId, cancellationToken);
        if (thread.IsLocked && !await CanManageAsync(userId, cancellationToken))
            throw new CampaignRuleException("The thread is locked.");
        var now = timeProvider.GetUtcNow();
        await AccountRestrictions.EnsureCanWriteAsync(db, userId, now, cancellationToken);
        if (replyToPostId is not null && !await db.Posts.AnyAsync(p => p.Id == replyToPostId && p.ThreadId == threadId, cancellationToken))
            throw new CampaignRuleException("The post you're replying to doesn't exist.");

        var post = Post.Create(threadId, userId, content, now, replyToPostId: replyToPostId);
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return post.Id;
    }

    public async Task EditPostAsync(long postId, string userId, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, thread) = await LoadPostAsync(db, postId, cancellationToken);
        if (post.AuthorId != userId || post.IsDeleted || post.IsHidden || thread.IsLocked)
            throw new CampaignRuleException("You can't edit this post.");
        var now = timeProvider.GetUtcNow();
        await AccountRestrictions.EnsureCanWriteAsync(db, userId, now, cancellationToken);

        db.PostRevisions.Add(post.Edit(content, now));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePostAsync(long postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, _) = await LoadPostAsync(db, postId, cancellationToken);
        if (post.AuthorId != userId && !await CanManageAsync(userId, cancellationToken))
            throw new CampaignRuleException("You can't delete this post.");

        post.Delete(userId, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SetPinnedAsync(int threadId, string userId, bool pinned, CancellationToken cancellationToken = default) =>
        ManageThreadAsync(threadId, userId, (thread, now) => thread.SetPinned(pinned, now), cancellationToken);

    public Task SetLockedAsync(int threadId, string userId, bool locked, CancellationToken cancellationToken = default) =>
        ManageThreadAsync(threadId, userId, (thread, now) => thread.SetLocked(locked, now), cancellationToken);

    public Task RenameThreadAsync(int threadId, string userId, string? title, CancellationToken cancellationToken = default) =>
        ManageThreadAsync(threadId, userId, (thread, now) => thread.Rename(title, now), cancellationToken);

    public async Task DeleteThreadAsync(int threadId, string userId, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var thread = await LoadThreadAsync(db, threadId, cancellationToken);

        // Inläggen, läspositionerna och rapporterna följer med (ON DELETE CASCADE).
        db.Threads.Remove(thread);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ManageThreadAsync(int threadId, string userId, Action<CampaignThread, DateTimeOffset> change, CancellationToken cancellationToken)
    {
        await EnsureCanManageAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var thread = await LoadThreadAsync(db, threadId, cancellationToken);
        change(thread, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    // Forumet sköts av sajtens administratörer och managers (B72).
    private async Task<bool> CanManageAsync(string userId, CancellationToken cancellationToken) =>
        userId.Length > 0 && SitePermissions.CanModerate(await siteRoles.GetRolesAsync(userId, cancellationToken));

    private async Task EnsureCanManageAsync(string userId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, cancellationToken))
            throw new CampaignRuleException("Only administrators and managers can do that in the forum.");
    }

    private static async Task<CampaignThread> LoadThreadAsync(TaleshavenDbContext db, int threadId, CancellationToken cancellationToken) =>
        await db.Threads.SingleOrDefaultAsync(t => t.Id == threadId && t.CategoryId != null, cancellationToken)
        ?? throw new CampaignRuleException("The thread doesn't exist.");

    private static async Task<(Post Post, CampaignThread Thread)> LoadPostAsync(TaleshavenDbContext db, long postId, CancellationToken cancellationToken)
    {
        var row = await (
                from p in db.Posts
                where p.Id == postId
                join t in db.Threads on p.ThreadId equals t.Id
                where t.CategoryId != null
                select new { Post = p, Thread = t })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("The post doesn't exist.");
        return (row.Post, row.Thread);
    }
}
