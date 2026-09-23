using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Threads;

internal sealed class ThreadService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : IThreadService
{
    public async Task<ThreadDetails?> GetChannelAsync(int campaignId, ThreadKind kind, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var thread = await db.Threads.AsNoTracking()
            .Where(t => t.CampaignId == campaignId && t.Kind == kind)
            .OrderBy(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (thread is null)
            return null;

        var access = await GetAccessAsync(db, campaignId, viewerId, cancellationToken);

        return new ThreadDetails(
            thread.Id,
            thread.CampaignId,
            thread.Kind,
            thread.Status,
            CanWrite: CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status));
    }

    public async Task<PostPage> GetInitialPostsAsync(int threadId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // Hämta en extra utöver maxgränsen för att veta om det finns äldre inlägg.
        var newest = await ToPostItemsAsync(db, db.Posts
            .Where(p => p.ThreadId == threadId)
            .OrderByDescending(p => p.Id)
            .Take(ChatWindow.InitialMaxPosts + 1), cancellationToken);

        var newestFirst = newest.Reverse().ToList();
        var count = ChatWindow.InitialCount(newestFirst.Select(p => p.CreatedAt).ToList(), timeProvider.GetUtcNow());

        return new PostPage(newest.Skip(newest.Count - count).ToList(), HasOlder: newest.Count > count);
    }

    public async Task<PostPage> GetPostsBeforeAsync(int threadId, long beforePostId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var older = await ToPostItemsAsync(db, db.Posts
            .Where(p => p.ThreadId == threadId && p.Id < beforePostId)
            .OrderByDescending(p => p.Id)
            .Take(ChatWindow.OlderPageSize + 1), cancellationToken);

        var hasOlder = older.Count > ChatWindow.OlderPageSize;
        return new PostPage(older.Skip(hasOlder ? 1 : 0).ToList(), hasOlder);
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
            ?? throw new CampaignRuleException("Kanalen finns inte.");

        var access = await GetAccessAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status))
            throw new CampaignRuleException("Du har inte behörighet att skriva här.");

        var post = Post.Create(threadId, userId, content, timeProvider.GetUtcNow());
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == post.Id), cancellationToken)).Single();
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
