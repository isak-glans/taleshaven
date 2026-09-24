using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Media;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Threads;

internal sealed class ThreadService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider, IDiceRoller diceRoller) : IThreadService
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

        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);

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

    public async Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content, int? characterId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var (thread, access) = await GetWritableThreadAsync(db, campaignId, threadId, userId, cancellationToken);

        if (characterId is not null)
        {
            if (thread.Kind != ThreadKind.Rpg)
                throw new CampaignRuleException("Karaktärer används bara i RPG.");

            var character = await db.Characters.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == characterId && c.CampaignId == campaignId, cancellationToken)
                ?? throw new CampaignRuleException("Karaktären finns inte.");

            if (!CampaignPermissions.CanPostAsCharacter(access.Role, userId, character.OwnerId, character.IsNpc))
                throw new CampaignRuleException("Du kan inte skriva som den karaktären.");
        }

        var post = Post.Create(threadId, userId, content, timeProvider.GetUtcNow(), characterId);
        return await SavePostAsync(db, post, cancellationToken);
    }

    public async Task<PostItem> EditPostAsync(int campaignId, long postId, string userId, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var post = await (
                from p in db.Posts
                join t in db.Threads on p.ThreadId equals t.Id
                where p.Id == postId && t.CampaignId == campaignId
                select p)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("Inlägget finns inte.");

        var thread = await db.Threads.AsNoTracking().SingleAsync(t => t.Id == post.ThreadId, cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanEditPost(access.Role, access.CampaignStatus, thread.Status, userId, post.AuthorId))
            throw new CampaignRuleException("Du kan bara redigera dina egna inlägg.");

        db.PostRevisions.Add(post.Edit(content, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == postId), cancellationToken)).Single();
    }

    public async Task<PostItem?> GetPostAsync(int threadId, long postId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == postId && p.ThreadId == threadId), cancellationToken))
            .SingleOrDefault();
    }

    public async Task<PostItem> RollDiceAsync(int campaignId, int threadId, string userId, DiceNotation notation, string? label, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var (thread, _) = await GetWritableThreadAsync(db, campaignId, threadId, userId, cancellationToken);
        if (thread.Kind != ThreadKind.Ooc)
            throw new CampaignRuleException("Tärningar kan bara slås i OOC.");

        // Kastet görs här på servern; klienten skickar bara vilken notation som ska slås.
        var roll = DiceRoll.Roll(notation, label, diceRoller);
        var post = Post.CreateDiceRoll(threadId, userId, roll, timeProvider.GetUtcNow());
        return await SavePostAsync(db, post, cancellationToken);
    }

    private static async Task<(CampaignThread Thread, (CampaignRole Role, CampaignStatus CampaignStatus) Access)> GetWritableThreadAsync(
        TaleshavenDbContext db, int campaignId, int threadId, string userId, CancellationToken cancellationToken)
    {
        var thread = await db.Threads.AsNoTracking().SingleOrDefaultAsync(t => t.Id == threadId && t.CampaignId == campaignId, cancellationToken)
            ?? throw new CampaignRuleException("Kanalen finns inte.");

        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status))
            throw new CampaignRuleException("Du har inte behörighet att skriva här.");

        return (thread, access);
    }

    private static async Task<PostItem> SavePostAsync(TaleshavenDbContext db, Post post, CancellationToken cancellationToken)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == post.Id), cancellationToken)).Single();
    }

    // Projektionen sker efter filtrering och sortering, och ordningen återställs i minnet (äldst först).
    private static async Task<IReadOnlyList<PostItem>> ToPostItemsAsync(TaleshavenDbContext db, IQueryable<Post> posts, CancellationToken cancellationToken)
    {
        var rows = await (
                from p in posts
                join u in db.Users on p.AuthorId equals u.Id
                join t in db.Threads on p.ThreadId equals t.Id
                join c in db.Campaigns on t.CampaignId equals c.Id
                join ch in db.Characters on p.CharacterId equals (int?)ch.Id into characters
                from ch in characters.DefaultIfEmpty()
                select new
                {
                    p.Id,
                    p.AuthorId,
                    u.DisplayName,
                    IsGameMaster = p.AuthorId == c.GameMasterId,
                    p.Content,
                    p.CreatedAt,
                    p.EditedAt,
                    p.Roll,
                    CharacterId = (int?)ch.Id,
                    CharacterName = ch.Name,
                    CharacterIsNpc = (bool?)ch.IsNpc,
                    CharacterAvatarKey = ch.AvatarKey,
                })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(r => r.Id)
            .Select(r => new PostItem(
                r.Id, r.AuthorId, r.DisplayName, r.IsGameMaster, r.Content, r.CreatedAt,
                r.Roll is null ? null : DiceRollView.From(r.Roll),
                r.CharacterId is not { } characterId ? null : new PostCharacter(
                    characterId, r.CharacterName!, r.CharacterIsNpc ?? false,
                    r.CharacterAvatarKey is null ? null : IImageStore.AvatarUrl(r.CharacterAvatarKey)),
                r.EditedAt))
            .ToList();
    }
}
