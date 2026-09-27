using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Threads;

internal sealed class UnreadService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : IUnreadService
{
    public async Task<long?> GetFirstUnreadPostIdAsync(int threadId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var campaignId = await db.Threads.Where(t => t.Id == threadId).Select(t => (int?)t.CampaignId).SingleOrDefaultAsync(cancellationToken);
        if (campaignId is null)
            return null;

        var access = await CampaignAccess.GetAsync(db, campaignId.Value, userId, cancellationToken);
        if (access.Role == CampaignRole.None)
            return null;

        var lastRead = await db.ReadMarkers
            .Where(m => m.ThreadId == threadId && m.UserId == userId)
            .Select(m => (long?)m.LastReadPostId)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;

        return await db.Posts
            .Where(p => p.ThreadId == threadId && p.Id > lastRead && p.AuthorId != userId && p.DeletedAt == null)
            .OrderBy(p => p.Id)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkReadAsync(int threadId, string userId, long lastPostId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var campaignId = await db.Threads.Where(t => t.Id == threadId).Select(t => (int?)t.CampaignId).SingleOrDefaultAsync(cancellationToken);
        if (campaignId is null)
            return;

        var access = await CampaignAccess.GetAsync(db, campaignId.Value, userId, cancellationToken);
        if (access.Role == CampaignRole.None)
            return;

        // Atomisk uppdatering som bara flyttar läspositionen framåt, även om flera flikar markerar samtidigt.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ReadMarkers" ("UserId", "ThreadId", "LastReadPostId", "UpdatedAt")
            VALUES ({userId}, {threadId}, {lastPostId}, {timeProvider.GetUtcNow()})
            ON CONFLICT ("UserId", "ThreadId") DO UPDATE
            SET "LastReadPostId" = GREATEST("ReadMarkers"."LastReadPostId", EXCLUDED."LastReadPostId"),
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetThreadUnreadAsync(int campaignId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (access.Role == CampaignRole.None)
            return new Dictionary<int, int>();

        var rows = await UnreadQueries.CountPerThread(db, userId, db.Threads.Where(t => t.CampaignId == campaignId))
            .ToListAsync(cancellationToken);
        return rows.Where(r => r.Count > 0).ToDictionary(r => r.ThreadId, r => r.Count);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetUnreadTotalsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var participating =
            from t in db.Threads
            join c in db.Campaigns on t.CampaignId equals c.Id
            where c.GameMasterId == userId || c.Memberships.Any(m => m.UserId == userId)
            select t;

        var rows = await UnreadQueries.CountPerThread(db, userId, participating).ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.CampaignId)
            .Select(g => (CampaignId: g.Key, Count: g.Sum(r => r.Count)))
            .Where(x => x.Count > 0)
            .ToDictionary(x => x.CampaignId, x => x.Count);
    }
}

internal static class UnreadQueries
{
    // Olästa = andras inlägg efter läspositionen som inte är borttagna. Utan läsposition räknas alla andras inlägg.
    public static IQueryable<UnreadRow> CountPerThread(TaleshavenDbContext db, string userId, IQueryable<CampaignThread> threads) =>
        from t in threads
        let lastRead = db.ReadMarkers
            .Where(m => m.UserId == userId && m.ThreadId == t.Id)
            .Select(m => (long?)m.LastReadPostId)
            .FirstOrDefault() ?? 0
        select new UnreadRow(
            t.Id,
            t.CampaignId,
            db.Posts.Count(p => p.ThreadId == t.Id && p.AuthorId != userId && p.Id > lastRead && p.DeletedAt == null));
}

internal sealed record UnreadRow(int ThreadId, int CampaignId, int Count);
/// <summary>Läspositioner som sätts av andra delar av systemet.</summary>
internal static class ReadMarkers
{
    /// <summary>
    /// Markerar allt som redan finns i kampanjens trådar som läst, t.ex. när en spelare godkänns.
    /// En ny spelare ska inte få hela kampanjens historik som "olästa".
    /// </summary>
    public static Task MarkAllReadAsync(TaleshavenDbContext db, int campaignId, string userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ReadMarkers" ("UserId", "ThreadId", "LastReadPostId", "UpdatedAt")
            SELECT {userId}, t."Id", COALESCE((SELECT max(p."Id") FROM "Posts" p WHERE p."ThreadId" = t."Id"), 0), {now}
            FROM "Threads" t
            WHERE t."CampaignId" = {campaignId}
            ON CONFLICT ("UserId", "ThreadId") DO NOTHING
            """, cancellationToken);
}
