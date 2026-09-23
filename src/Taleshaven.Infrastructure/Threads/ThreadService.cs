using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Threads;

internal sealed class ThreadService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : IThreadService
{
    public async Task<IReadOnlyList<ThreadListItem>> GetRpgThreadsAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await (
                from t in db.Threads.AsNoTracking()
                where t.CampaignId == campaignId && t.Kind == ThreadKind.Rpg
                orderby t.CreatedAt descending
                select new ThreadListItem(
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    db.Posts.Count(p => p.ThreadId == t.Id),
                    db.Posts.Where(p => p.ThreadId == t.Id).Max(p => (DateTimeOffset?)p.CreatedAt),
                    t.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateRpgThreadAsync(int campaignId, string userId, NewThread thread, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var access = await GetAccessAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanManageThreads(access.Role))
            throw new CampaignRuleException("Endast kampanjens GM kan skapa trådar.");

        var entity = CampaignThread.CreateRpg(campaignId, thread.Title, thread.Description, userId, timeProvider.GetUtcNow());
        db.Threads.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task SetThreadLockedAsync(int campaignId, int threadId, string userId, bool locked, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var access = await GetAccessAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanManageThreads(access.Role))
            throw new CampaignRuleException("Endast kampanjens GM kan låsa trådar.");

        var thread = await db.Threads.SingleOrDefaultAsync(t => t.Id == threadId && t.CampaignId == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("Tråden finns inte.");

        thread.SetLocked(locked);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<ThreadDetails?> GetThreadAsync(int campaignId, int threadId, string viewerId, CancellationToken cancellationToken = default) =>
        GetThreadDetailsAsync(campaignId, viewerId, t => t.Id == threadId, cancellationToken);

    public Task<ThreadDetails?> GetOocThreadAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default) =>
        GetThreadDetailsAsync(campaignId, viewerId, t => t.Kind == ThreadKind.Ooc, cancellationToken);

    public async Task<IReadOnlyList<PostItem>> GetLatestPostsAsync(int threadId, int count, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var page = db.Posts
            .Where(p => p.ThreadId == threadId)
            .OrderByDescending(p => p.Id)
            .Take(count);

        return await ToPostItemsAsync(db, page, cancellationToken);
    }

    public async Task<IReadOnlyList<PostItem>> GetPostsBeforeAsync(int threadId, long beforePostId, int count, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var page = db.Posts
            .Where(p => p.ThreadId == threadId && p.Id < beforePostId)
            .OrderByDescending(p => p.Id)
            .Take(count);

        return await ToPostItemsAsync(db, page, cancellationToken);
    }

    public async Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await ToPostItemsAsync(db, db.Posts.Where(p => p.ThreadId == threadId && p.Id > afterPostId), cancellationToken);
    }

    public async Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var thread = await db.Threads.AsNoTracking().SingleOrDefaultAsync(t => t.Id == threadId && t.CampaignId == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("Tråden finns inte.");

        var access = await GetAccessAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status))
            throw new CampaignRuleException(thread.Status == ThreadStatus.Locked
                ? "Tråden är låst."
                : "Du har inte behörighet att skriva här.");

        var post = Post.Create(threadId, userId, content, timeProvider.GetUtcNow());
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == post.Id), cancellationToken)).Single();
    }

    private async Task<ThreadDetails?> GetThreadDetailsAsync(
        int campaignId,
        string viewerId,
        System.Linq.Expressions.Expression<Func<CampaignThread, bool>> predicate,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var thread = await db.Threads.AsNoTracking()
            .Where(t => t.CampaignId == campaignId)
            .Where(predicate)
            .SingleOrDefaultAsync(cancellationToken);

        if (thread is null)
            return null;

        var access = await GetAccessAsync(db, campaignId, viewerId, cancellationToken);

        return new ThreadDetails(
            thread.Id,
            thread.CampaignId,
            thread.Kind,
            thread.Title,
            thread.Description,
            thread.Status,
            CanWrite: CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status),
            CanManage: thread.Kind == ThreadKind.Rpg && CampaignPermissions.CanManageThreads(access.Role));
    }

    private static async Task<(CampaignRole Role, CampaignStatus CampaignStatus)> GetAccessAsync(
        TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var access = await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId)
            .Select(c => new
            {
                c.Status,
                Role = c.GameMasterId == userId ? CampaignRole.GameMaster
                    : c.Memberships.Any(m => m.UserId == userId) ? CampaignRole.Player
                    : CampaignRole.None,
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("Kampanjen finns inte.");

        return (access.Role, access.Status);
    }

    // Projektionen sker efter filtrering och sortering, och ordningen återställs i minnet (äldst först).
    private static async Task<IReadOnlyList<PostItem>> ToPostItemsAsync(TaleshavenDbContext db, IQueryable<Post> posts, CancellationToken cancellationToken)
    {
        var items = await (
                from p in posts
                join u in db.Users on p.AuthorId equals u.Id
                join t in db.Threads on p.ThreadId equals t.Id
                join c in db.Campaigns on t.CampaignId equals c.Id
                select new PostItem(p.Id, p.AuthorId, u.DisplayName, p.AuthorId == c.GameMasterId, p.Content, p.CreatedAt))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return items.OrderBy(p => p.Id).ToList();
    }
}
