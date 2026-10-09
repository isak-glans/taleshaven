using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Threads;

internal sealed class ThreadService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider, IDiceRoller diceRoller) : IThreadService
{
    public async Task<IReadOnlyList<ThreadSummary>> GetThreadsAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);

        var threads = db.Threads.Where(t => t.CampaignId == campaignId);
        var rows = await threads
            .AsNoTracking()
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Status,
                t.Position,
                PostCount = db.Posts.Count(p => p.ThreadId == t.Id),
                ParticipantCount = db.Posts.Where(p => p.ThreadId == t.Id).Select(p => p.AuthorId).Distinct().Count(),
                LastPostId = db.Posts.Where(p => p.ThreadId == t.Id).Max(p => (long?)p.Id),
            })
            .ToListAsync(cancellationToken);

        var lastPostIds = rows.Where(r => r.LastPostId is not null).Select(r => r.LastPostId!.Value).ToList();
        var lastPosts = (await ToPostItemsAsync(db, db.Posts.Where(p => lastPostIds.Contains(p.Id)), Viewer.ReadOnly(viewerId), cancellationToken))
            .ToDictionary(p => p.Id);

        var unread = access.Role == CampaignRole.None
            ? new Dictionary<int, int>()
            : await UnreadQueries.CountPerThread(db, viewerId, threads).ToDictionaryAsync(r => r.ThreadId, r => r.Count, cancellationToken);

        // Aktiva först i GM:s ordning; avslutade sedan, senast avslutade kapitlet överst (B27).
        return rows
            .OrderBy(r => r.Status)
            .ThenBy(r => r.Status == ThreadStatus.Active ? r.Position : -r.Position)
            .ThenBy(r => r.Id)
            .Select(r =>
            {
                var last = r.LastPostId is { } id && lastPosts.TryGetValue(id, out var post) ? post : null;
                return new ThreadSummary(
                    r.Id, r.Title, r.Status, r.PostCount, r.ParticipantCount,
                    last is null ? null : AuthorOf(last), last?.CreatedAt, unread.GetValueOrDefault(r.Id));
            })
            .ToList();
    }

    public async Task<ThreadDetails?> GetThreadAsync(int campaignId, int threadId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var row = await db.Threads.AsNoTracking()
            .Where(t => t.Id == threadId && t.CampaignId == campaignId)
            .Select(t => new
            {
                Thread = t,

                PostCount = db.Posts.Count(p => p.ThreadId == t.Id),
                ParticipantCount = db.Posts.Where(p => p.ThreadId == t.Id).Select(p => p.AuthorId).Distinct().Count(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);
        var thread = row.Thread;
        return new ThreadDetails(
            thread.Id, thread.CampaignId, thread.Status, thread.Title, thread.CreatedAt, row.PostCount, row.ParticipantCount,
            CanWrite: CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status),
            CanManage: CampaignPermissions.CanManageThreads(access.Role));
    }

    public async Task<PostPage> GetPostsPageAsync(int threadId, int page, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var viewer = await ViewerForThreadAsync(db, threadId, viewerId, cancellationToken);

        var posts = db.Posts.Where(p => p.ThreadId == threadId);
        var total = await posts.CountAsync(cancellationToken);
        var pageCount = Paging.PageCount(total);
        var shown = Paging.Clamp(page, pageCount);

        var items = await ToPostItemsAsync(db, posts
            .OrderBy(p => p.Id)
            .Skip((shown - 1) * ThreadLimits.PostsPerPage)
            .Take(ThreadLimits.PostsPerPage), viewer, cancellationToken);

        return new PostPage(items, shown, pageCount, total);
    }

    public async Task<int?> GetPageOfPostAsync(int threadId, long postId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (!await db.Posts.AnyAsync(p => p.Id == postId && p.ThreadId == threadId, cancellationToken))
            return null;

        var position = await db.Posts.CountAsync(p => p.ThreadId == threadId && p.Id <= postId, cancellationToken);
        return Paging.PageOf(position);
    }

    public async Task<IReadOnlyList<PostItem>> GetPostsAfterAsync(int threadId, long afterPostId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var viewer = await ViewerForThreadAsync(db, threadId, viewerId, cancellationToken);

        return await ToPostItemsAsync(db, db.Posts.Where(p => p.ThreadId == threadId && p.Id > afterPostId), viewer, cancellationToken);
    }

    public async Task<PostItem?> GetPostAsync(int threadId, long postId, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var viewer = await ViewerForThreadAsync(db, threadId, viewerId, cancellationToken);

        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == postId && p.ThreadId == threadId), viewer, cancellationToken))
            .SingleOrDefault();
    }

    public async Task<PostItem> CreatePostAsync(int campaignId, int threadId, string userId, string? content,
        int? characterId = null, long? replyToPostId = null, IReadOnlyList<RollRequest>? rolls = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var thread = await LoadThreadAsync(db, campaignId, threadId, cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanWritePost(access.Role, access.CampaignStatus, thread.Status))
            throw new CampaignRuleException(thread.Status == ThreadStatus.Completed
                ? "The thread is completed."
                : "You don't have permission to post here.");

        if (characterId is not null)
        {
            var character = await db.Characters.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == characterId && c.CampaignId == campaignId, cancellationToken)
                ?? throw new CampaignRuleException("The character doesn't exist.");

            if (!CampaignPermissions.CanPostAsCharacter(access.Role, userId, character.OwnerId, character.IsNpc))
                throw new CampaignRuleException("You can't post as that character.");
            if (character.IsArchived)
                throw new CampaignRuleException($"{character.Name} is archived. Restore the NPC under Characters to post as it.");
        }

        if (replyToPostId is not null && !await db.Posts.AnyAsync(p => p.Id == replyToPostId && p.ThreadId == threadId, cancellationToken))
            throw new CampaignRuleException("The post you're replying to doesn't exist.");

        var post = Post.Create(threadId, userId, content, timeProvider.GetUtcNow(), characterId, replyToPostId,
            await WithIconsAsync(db, rolls, cancellationToken), diceRoller);
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        var viewer = new Viewer(userId, access.Role, access.CampaignStatus, thread.Status);
        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == post.Id), viewer, cancellationToken)).Single();
    }

    // Slagens ikoner (B60): en ikon från ett sparat slag måste finnas och vara en ikon, annars tas den bort;
    // ett slag utan ikon men med beskrivning får ett förslag efter beskrivningen, som på karaktären (B58).
    private static async Task<IReadOnlyList<RollRequest>?> WithIconsAsync(
        TaleshavenDbContext db, IReadOnlyList<RollRequest>? rolls, CancellationToken cancellationToken)
    {
        if (rolls is not { Count: > 0 })
            return rolls;

        var icons = await db.Portraits.AsNoTracking()
            .Where(p => p.Kind == ImageKind.Icon)
            .Select(p => new { p.Id, p.Tags })
            .ToListAsync(cancellationToken);
        var candidates = icons.Select(i => (i.Id, (IReadOnlyList<string>)i.Tags)).ToList();
        var iconIds = icons.Select(i => i.Id).ToHashSet();

        return rolls.Select(r => r with
        {
            IconId = r.IconId is { } id && iconIds.Contains(id) ? id
                : string.IsNullOrWhiteSpace(r.Label) ? null
                : IconMatcher.Suggest(r.Label, candidates),
        }).ToList();
    }

    public async Task<PostItem> EditPostAsync(int campaignId, long postId, string userId, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, thread, access) = await LoadPostAsync(db, campaignId, postId, userId, cancellationToken);

        if (!CampaignPermissions.CanEditPost(access.Role, access.CampaignStatus, thread.Status, userId, post.AuthorId))
            throw new CampaignRuleException("You can't edit this post.");

        db.PostRevisions.Add(post.Edit(content, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);

        var viewer = new Viewer(userId, access.Role, access.CampaignStatus, thread.Status);
        return (await ToPostItemsAsync(db, db.Posts.Where(p => p.Id == postId), viewer, cancellationToken)).Single();
    }

    public async Task DeletePostAsync(int campaignId, long postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (post, thread, access) = await LoadPostAsync(db, campaignId, postId, userId, cancellationToken);

        if (!CampaignPermissions.CanDeletePost(access.Role, access.CampaignStatus, thread.Status, userId, post.AuthorId, post.HasRolls))
            throw new CampaignRuleException(post.HasRolls && post.AuthorId == userId
                ? "Posts with dice rolls can only be deleted by the GM."
                : "You can't delete this post.");

        post.Delete(userId, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostingChoice> GetLastPostingChoiceAsync(int threadId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var last = await db.Posts.AsNoTracking()
            .Where(p => p.ThreadId == threadId && p.AuthorId == userId)
            .OrderByDescending(p => p.Id)
            .Select(p => new { p.CharacterId })
            .FirstOrDefaultAsync(cancellationToken);

        return last is null ? new PostingChoice(false, null) : new PostingChoice(true, last.CharacterId);
    }

    public async Task<int> CreateThreadAsync(int campaignId, string userId, string? title, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanManageAsync(db, campaignId, userId, cancellationToken);

        var position = (await db.Threads.Where(t => t.CampaignId == campaignId).MaxAsync(t => (int?)t.Position, cancellationToken) ?? 0) + 1;
        var thread = CampaignThread.Create(campaignId, title, position, userId, timeProvider.GetUtcNow());

        db.Threads.Add(thread);
        await db.SaveChangesAsync(cancellationToken);
        return thread.Id;
    }

    public Task RenameThreadAsync(int campaignId, int threadId, string userId, string? title, CancellationToken cancellationToken = default) =>
        ChangeThreadAsync(campaignId, threadId, userId, thread => thread.Rename(title, timeProvider.GetUtcNow()), cancellationToken);

    public Task CompleteThreadAsync(int campaignId, int threadId, string userId, CancellationToken cancellationToken = default) =>
        ChangeThreadAsync(campaignId, threadId, userId, thread => thread.Complete(timeProvider.GetUtcNow()), cancellationToken);

    public Task ReopenThreadAsync(int campaignId, int threadId, string userId, CancellationToken cancellationToken = default) =>
        ChangeThreadAsync(campaignId, threadId, userId, thread => thread.Reopen(timeProvider.GetUtcNow()), cancellationToken);
    public async Task MoveThreadAsync(int campaignId, int threadId, string userId, int direction, CancellationToken cancellationToken = default)
    {
        if (direction is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Riktningen måste vara -1 eller 1.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanManageAsync(db, campaignId, userId, cancellationToken);

        var thread = await LoadThreadAsync(db, campaignId, threadId, cancellationToken);

        // Samma ordning som i listan: aktiva stigande, avslutade fallande (B27). Grannarna byter position.
        var group = (await db.Threads.Where(t => t.CampaignId == campaignId && t.Status == thread.Status).ToListAsync(cancellationToken))
            .OrderBy(t => thread.Status == ThreadStatus.Active ? t.Position : -t.Position)
            .ThenBy(t => t.Id)
            .ToList();

        var index = group.IndexOf(thread);
        var neighbourIndex = index + direction;
        if (neighbourIndex < 0 || neighbourIndex >= group.Count)
            return;

        // Positionerna görs först unika i visningsordning, så att bytet alltid flyttar tråden.
        for (var i = 0; i < group.Count; i++)
            group[i].MoveTo(thread.Status == ThreadStatus.Active ? i + 1 : group.Count - i);

        var neighbour = group[neighbourIndex];
        var position = thread.Position;
        thread.MoveTo(neighbour.Position);
        neighbour.MoveTo(position);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ChangeThreadAsync(int campaignId, int threadId, string userId, Action<CampaignThread> change, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanManageAsync(db, campaignId, userId, cancellationToken);

        var thread = await LoadThreadAsync(db, campaignId, threadId, cancellationToken);
        change(thread);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCanManageAsync(TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanManageThreads(access.Role))
            throw new CampaignRuleException("Only the campaign's GM can manage threads.");
    }

    private static async Task<CampaignThread> LoadThreadAsync(TaleshavenDbContext db, int campaignId, int threadId, CancellationToken cancellationToken) =>
        await db.Threads.SingleOrDefaultAsync(t => t.Id == threadId && t.CampaignId == campaignId, cancellationToken)
        ?? throw new CampaignRuleException("The thread doesn't exist.");

    private static async Task<(Post Post, CampaignThread Thread, (CampaignRole Role, CampaignStatus CampaignStatus) Access)> LoadPostAsync(
        TaleshavenDbContext db, int campaignId, long postId, string userId, CancellationToken cancellationToken)
    {
        var post = await (
                from p in db.Posts
                join t in db.Threads on p.ThreadId equals t.Id
                where p.Id == postId && t.CampaignId == campaignId
                select p)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("The post doesn't exist.");

        var thread = await db.Threads.AsNoTracking().SingleAsync(t => t.Id == post.ThreadId, cancellationToken);
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        return (post, thread, access);
    }

    private static async Task<Viewer> ViewerForThreadAsync(TaleshavenDbContext db, int threadId, string viewerId, CancellationToken cancellationToken)
    {
        var thread = await db.Threads.AsNoTracking()
            .Where(t => t.Id == threadId)
            .Select(t => new { t.CampaignId, t.Status })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CampaignRuleException("The thread doesn't exist.");

        var access = await CampaignAccess.GetAsync(db, thread.CampaignId, viewerId, cancellationToken);
        return new Viewer(viewerId, access.Role, access.CampaignStatus, thread.Status);
    }

    private static PostAuthor AuthorOf(PostItem post) => post.Character is { } character
        ? new PostAuthor(character.Name, character.AvatarUrl, $"character-{character.Id}", character.IsHidden && character.AvatarUrl is null)
        : new PostAuthor(post.AuthorName, post.AuthorAvatarUrl, post.AuthorId, false);

    /// <summary>Den som läser, för att maskera dolda NPC:er och räkna ut vad hen får göra med varje inlägg.</summary>
    private sealed record Viewer(string UserId, CampaignRole Role, CampaignStatus CampaignStatus, ThreadStatus ThreadStatus)
    {
        /// <summary>För listor där inga knappar visas; bara maskeringen spelar roll.</summary>
        public static Viewer ReadOnly(string userId) => new(userId, CampaignRole.None, CampaignStatus.Archived, ThreadStatus.Completed);
    }

    // Projektionen sker efter filtrering och sortering, och ordningen återställs i minnet (äldst först).
    // Viewer.Role används bara för behörigheter; om läsaren är GM avgörs per kampanj i frågan (för trådlistan).
    private static async Task<IReadOnlyList<PostItem>> ToPostItemsAsync(
        TaleshavenDbContext db, IQueryable<Post> posts, Viewer viewer, CancellationToken cancellationToken)
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
                    p.ThreadId,
                    p.AuthorId,
                    u.DisplayName,
                    AuthorAvatarKey = db.Portraits.Where(pt => pt.Id == u.PortraitId).Select(pt => pt.ImageKey).FirstOrDefault(),
                    AuthorIsDeleted = u.UserName == "deleted-" + u.Id,
                    IsGameMaster = p.AuthorId == c.GameMasterId,
                    ViewerIsGameMaster = c.GameMasterId == viewer.UserId,
                    p.Content,
                    p.CreatedAt,
                    p.EditedAt,
                    p.DeletedAt,
                    p.Rolls,
                    p.ReplyToPostId,
                    CharacterId = (int?)ch.Id,
                    CharacterName = ch.Name,
                    CharacterIsNpc = (bool?)ch.IsNpc,
                    CharacterAvatarKey = db.Portraits.Where(pt => pt.Id == ch.PortraitId).Select(pt => pt.ImageKey).FirstOrDefault(),
                    CharacterIsHidden = (bool?)ch.IsHidden,
                    CharacterAlias = ch.Alias,
                    p.DeletedCharacterName,
                })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var replyIds = rows.Where(r => r.ReplyToPostId is not null).Select(r => r.ReplyToPostId!.Value).Distinct().ToList();
        var replies = replyIds.Count == 0
            ? new Dictionary<long, string>()
            : (await (
                    from p in db.Posts
                    where replyIds.Contains(p.Id)
                    join u in db.Users on p.AuthorId equals u.Id
                    join t in db.Threads on p.ThreadId equals t.Id
                    join c in db.Campaigns on t.CampaignId equals c.Id
                    join ch in db.Characters on p.CharacterId equals (int?)ch.Id into characters
                    from ch in characters.DefaultIfEmpty()
                    select new
                    {
                        p.Id,
                        u.DisplayName,
                        ViewerIsGameMaster = c.GameMasterId == viewer.UserId,
                        CharacterName = ch.Name,
                        CharacterIsHidden = (bool?)ch.IsHidden,
                        CharacterAlias = ch.Alias,
                        p.DeletedCharacterName,
                    })
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .ToDictionary(
                r => r.Id,
                r => r.CharacterName is null ? r.DeletedCharacterName ?? r.DisplayName
                    : r.ViewerIsGameMaster ? r.CharacterName
                    : Core.Characters.Character.NameForPlayers(r.CharacterName, r.CharacterIsHidden ?? false, r.CharacterAlias));

        // Slagens ikoner (B60). En ikon som tagits bort ur biblioteket saknas här, och slaget visas med 🎲.
        var iconIds = rows.SelectMany(r => r.Rolls).Select(roll => roll.IconId).OfType<int>().Distinct().ToList();
        var iconUrls = iconIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Portraits.AsNoTracking()
                .Where(p => iconIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => IImageStore.PortraitUrl(p.ImageKey), cancellationToken);
        DiceRollView View(DiceRoll roll) =>
            DiceRollView.From(roll, roll.IconId is { } id ? iconUrls.GetValueOrDefault(id) : null);

        return rows
            .OrderBy(r => r.Id)
            .Select(r =>
            {
                var deleted = r.DeletedAt is not null;
                var hasRolls = r.Rolls.Count > 0;
                return new PostItem(
                    r.Id, r.ThreadId, r.AuthorId, r.DisplayName, r.IsGameMaster,
                    // Texten i ett borttaget inlägg lämnar aldrig servern (B30).
                    Content: deleted ? "" : r.Content,
                    r.CreatedAt, r.EditedAt,
                    Rolls: deleted ? [] : r.Rolls.Select(View).ToList(),
                    Character: r.CharacterId is not { } characterId
                        ? r.DeletedCharacterName is { } deletedName ? PostCharacter.Deleted(deletedName) : null
                        : PostCharacter.ForViewer(
                            characterId, r.CharacterName!, r.CharacterIsNpc ?? false,
                            r.CharacterAvatarKey is null ? null : IImageStore.PortraitUrl(r.CharacterAvatarKey),
                            r.CharacterIsHidden ?? false, r.CharacterAlias, r.ViewerIsGameMaster),
                    ReplyTo: r.ReplyToPostId is { } replyId && replies.TryGetValue(replyId, out var replyName)
                        ? new PostReference(replyId, replyName)
                        : null,
                    IsDeleted: deleted,
                    CanEdit: !deleted && CampaignPermissions.CanEditPost(viewer.Role, viewer.CampaignStatus, viewer.ThreadStatus, viewer.UserId, r.AuthorId),
                    CanDelete: !deleted && CampaignPermissions.CanDeletePost(viewer.Role, viewer.CampaignStatus, viewer.ThreadStatus, viewer.UserId, r.AuthorId, hasRolls),
                    // Profilbilden visas när inlägget är skrivet utan karaktär, t.ex. GM som berättare (B50).
                    AuthorAvatarUrl: r.AuthorAvatarKey is null ? null : IImageStore.PortraitUrl(r.AuthorAvatarKey),
                    // Borttagna konton (B21) har ingen profilsida att länka till (B52).
                    AuthorIsDeleted: r.AuthorIsDeleted);
            })
            .ToList();
    }
}
