using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Core.Messages;
using Taleshaven.Core.Threads;
using Taleshaven.Core.Users;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Moderation;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure.Messages;

internal sealed class MessageService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : IMessageService
{
    /// <summary>Hur mycket av senaste meddelandet som visas i listan.</summary>
    private const int SnippetLength = 90;

    private static readonly Regex SpoilerPattern = new(@"\[spoiler(=[^\]]*)?\].*?(\[/spoiler\]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex MarkupPattern = new(@"\*\*|__|~~|`|^\s*(#+|>+|[-*])\s", RegexOptions.Multiline);

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await (
                from c in db.Conversations.AsNoTracking()
                where c.UserAId == userId || c.UserBId == userId
                let otherId = c.UserAId == userId ? c.UserBId : c.UserAId
                join u in db.Users on otherId equals u.Id
                let last = db.Posts.Where(p => p.ThreadId == c.ThreadId).OrderByDescending(p => p.Id).FirstOrDefault()
                where last != null
                select new
                {
                    c.Id,
                    c.ThreadId,
                    OtherId = u.Id,
                    u.DisplayName,
                    u.UserName,
                    AvatarKey = db.Portraits.Where(p => p.Id == u.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
                    LastContent = last!.DeletedAt == null && last.HiddenAt == null ? last.Content : null,
                    LastAuthorId = last.AuthorId,
                    LastAt = last.CreatedAt,
                })
            .ToListAsync(cancellationToken);

        var threadIds = rows.Select(r => r.ThreadId).ToList();
        var unread = (await UnreadQueries.CountPerThread(db, userId, db.Threads.Where(t => threadIds.Contains(t.Id))).ToListAsync(cancellationToken))
            .ToDictionary(r => r.ThreadId, r => r.Count);

        return rows
            .OrderByDescending(r => r.LastAt)
            .Select(r =>
            {
                var deleted = DeletedAccount.IsTombstone(r.OtherId, r.UserName);
                return new ConversationSummary(r.Id, r.OtherId,
                    deleted ? DeletedAccount.DisplayName : r.DisplayName,
                    r.AvatarKey is { } key && !deleted ? IImageStore.PortraitUrl(key) : null,
                    deleted, Snippet(r.LastContent), r.LastAuthorId == userId, r.LastAt, unread.GetValueOrDefault(r.ThreadId));
            })
            .ToList();
    }

    public async Task<ConversationDetails?> GetConversationAsync(int conversationId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var conversation = await db.Conversations.AsNoTracking().SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null || !conversation.Includes(userId))
            return null;

        var otherId = conversation.OtherThan(userId);
        var other = await LoadPersonAsync(db, otherId, cancellationToken);
        var (youBlocked, blockedYou) = await BlocksAsync(db, userId, otherId, cancellationToken);
        return new ConversationDetails(conversation.Id, conversation.ThreadId, otherId, other.Name, other.AvatarUrl, other.IsDeleted,
            youBlocked, blockedYou, CanWrite: !other.IsDeleted && !youBlocked && !blockedYou);
    }

    public async Task<int?> FindConversationAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await FindAsync(db, userId, otherUserId, cancellationToken);
    }

    public async Task<MessageRecipient?> GetRecipientAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
            return null;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Users.AnyAsync(u => u.Id == otherUserId, cancellationToken))
            return null;
        var other = await LoadPersonAsync(db, otherUserId, cancellationToken);
        if (other.IsDeleted)
            return null;

        var (youBlocked, blockedYou) = await BlocksAsync(db, userId, otherUserId, cancellationToken);
        return new MessageRecipient(otherUserId, other.Name, other.AvatarUrl, await FindAsync(db, userId, otherUserId, cancellationToken),
            youBlocked, blockedYou);
    }

    public async Task<SentMessage> SendAsync(string senderId, string recipientId, string? content, CancellationToken cancellationToken = default)
    {
        if (senderId == recipientId)
            throw new CampaignRuleException("You can't send a message to yourself.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        await AccountRestrictions.EnsureCanWriteAsync(db, senderId, now, cancellationToken);

        var recipient = await db.Users.AsNoTracking()
            .Where(u => u.Id == recipientId)
            .Select(u => new { u.Id, u.UserName, u.DisplayName, u.Email, u.EmailConfirmed, u.EmailOnMessage })
            .SingleOrDefaultAsync(cancellationToken);
        if (recipient is null || DeletedAccount.IsTombstone(recipient.Id, recipient.UserName))
            throw new CampaignRuleException("The user doesn't exist.");
        var (youBlocked, blockedYou) = await BlocksAsync(db, senderId, recipientId, cancellationToken);
        if (youBlocked)
            throw new CampaignRuleException("You have blocked this user. Unblock them to send a message.");
        if (blockedYou)
            throw new CampaignRuleException("You can't send messages to this user.");
        var senderName = await db.Users.Where(u => u.Id == senderId).Select(u => u.DisplayName).SingleAsync(cancellationToken);

        // Konversationen skapas med första meddelandet. Skriver båda samtidigt stoppar det unika indexet den ena, som
        // får försöka igen (och då hittar konversationen).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var (a, b) = Conversation.Pair(senderId, recipientId);
        var conversation = await db.Conversations.SingleOrDefaultAsync(c => c.UserAId == a && c.UserBId == b, cancellationToken);
        if (conversation is null)
        {
            var thread = CampaignThread.CreateConversationThread(senderId, now);
            db.Threads.Add(thread);
            await db.SaveChangesAsync(cancellationToken);
            conversation = Conversation.Create(thread.Id, senderId, recipientId, now);
            db.Conversations.Add(conversation);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new CampaignRuleException("The message couldn't be sent. Please try again.");
            }
        }

        // Ett mejl per omgång: bara om mottagaren inte redan hade olästa meddelanden i konversationen.
        var hadUnread = (await UnreadQueries.CountPerThread(db, recipientId, db.Threads.Where(t => t.Id == conversation.ThreadId))
            .ToListAsync(cancellationToken)).Any(r => r.Count > 0);

        var post = Post.Create(conversation.ThreadId, senderId, content, now);
        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var notify = recipient.EmailOnMessage && recipient.EmailConfirmed && !hadUnread ? recipient.Email : null;
        return new SentMessage(conversation.Id, conversation.ThreadId, post.Id, senderName, recipient.DisplayName, notify);
    }

    public async Task EditAsync(long postId, string userId, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var post = await LoadOwnMessageAsync(db, postId, userId, cancellationToken);
        if (post.IsDeleted || post.IsHidden)
            throw new CampaignRuleException("You can't edit this message.");
        var now = timeProvider.GetUtcNow();
        await AccountRestrictions.EnsureCanWriteAsync(db, userId, now, cancellationToken);

        db.PostRevisions.Add(post.Edit(content, now));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long postId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var post = await LoadOwnMessageAsync(db, postId, userId, cancellationToken);
        if (post.IsDeleted)
            return;
        post.Delete(userId, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task BlockAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
            throw new CampaignRuleException("You can't block yourself.");
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Users.AnyAsync(u => u.Id == otherUserId, cancellationToken))
            throw new CampaignRuleException("The user doesn't exist.");
        if (await db.UserBlocks.AnyAsync(b => b.BlockerId == userId && b.BlockedId == otherUserId, cancellationToken))
            return;
        db.UserBlocks.Add(UserBlock.Create(userId, otherUserId, timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.UserBlocks.Where(b => b.BlockerId == userId && b.BlockedId == otherUserId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BlockedUser>> GetBlockedAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await (
                from b in db.UserBlocks.AsNoTracking()
                where b.BlockerId == userId
                join u in db.Users on b.BlockedId equals u.Id
                orderby u.DisplayName
                select new
                {
                    u.Id,
                    u.UserName,
                    u.DisplayName,
                    AvatarKey = db.Portraits.Where(p => p.Id == u.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
                    b.CreatedAt,
                })
            .ToListAsync(cancellationToken);
        return rows
            .Select(r =>
            {
                var deleted = DeletedAccount.IsTombstone(r.Id, r.UserName);
                return new BlockedUser(r.Id, deleted ? DeletedAccount.DisplayName : r.DisplayName,
                    r.AvatarKey is { } key && !deleted ? IImageStore.PortraitUrl(key) : null, r.CreatedAt);
            })
            .ToList();
    }

    public async Task<int> CountUnreadConversationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var threads = db.Threads.Where(t => db.Conversations.Any(c => c.ThreadId == t.Id && (c.UserAId == userId || c.UserBId == userId)));
        return (await UnreadQueries.CountPerThread(db, userId, threads).ToListAsync(cancellationToken)).Count(r => r.Count > 0);
    }

    public async Task<bool> GetEmailOnMessageAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.Where(u => u.Id == userId).Select(u => u.EmailOnMessage).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SetEmailOnMessageAsync(string userId, bool enabled, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailOnMessage, enabled), cancellationToken);
    }

    private static Task<int?> FindAsync(TaleshavenDbContext db, string userId, string otherUserId, CancellationToken cancellationToken)
    {
        var (a, b) = Conversation.Pair(userId, otherUserId);
        return db.Conversations.Where(c => c.UserAId == a && c.UserBId == b).Select(c => (int?)c.Id).SingleOrDefaultAsync(cancellationToken);
    }

    private static async Task<(bool YouBlocked, bool BlockedYou)> BlocksAsync(TaleshavenDbContext db, string userId, string otherUserId,
        CancellationToken cancellationToken)
    {
        var blockers = await db.UserBlocks
            .Where(b => (b.BlockerId == userId && b.BlockedId == otherUserId) || (b.BlockerId == otherUserId && b.BlockedId == userId))
            .Select(b => b.BlockerId)
            .ToListAsync(cancellationToken);
        return (blockers.Contains(userId), blockers.Contains(otherUserId));
    }

    private sealed record Person(string Name, string? AvatarUrl, bool IsDeleted);

    private static async Task<Person> LoadPersonAsync(TaleshavenDbContext db, string userId, CancellationToken cancellationToken)
    {
        var row = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.DisplayName,
                AvatarKey = db.Portraits.Where(p => p.Id == u.PortraitId).Select(p => p.ImageKey).FirstOrDefault(),
            })
            .SingleAsync(cancellationToken);
        var deleted = DeletedAccount.IsTombstone(row.Id, row.UserName);
        return new Person(deleted ? DeletedAccount.DisplayName : row.DisplayName,
            row.AvatarKey is { } key && !deleted ? IImageStore.PortraitUrl(key) : null, deleted);
    }

    // Bara avsändarens egna meddelanden, i en konversation där hen är med.
    private static async Task<Post> LoadOwnMessageAsync(TaleshavenDbContext db, long postId, string userId, CancellationToken cancellationToken)
    {
        var post = await (
                from p in db.Posts
                where p.Id == postId && p.AuthorId == userId
                where db.Conversations.Any(c => c.ThreadId == p.ThreadId)
                select p)
            .SingleOrDefaultAsync(cancellationToken);
        return post ?? throw new CampaignRuleException("You can't change this message.");
    }

    // Listans utdrag ur senaste meddelandet: utan spoilerns innehåll och utan de vanligaste Markdown-tecknen.
    private static string? Snippet(string? content)
    {
        if (content is null)
            return null;
        var text = MarkupPattern.Replace(SpoilerPattern.Replace(content, "[spoiler]"), "");
        var flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length <= SnippetLength ? flat : flat[..SnippetLength].TrimEnd() + "…";
    }
}
