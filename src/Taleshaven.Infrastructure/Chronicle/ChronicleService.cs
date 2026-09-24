using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Chronicle;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Chronicle;

internal sealed class ChronicleService(IDbContextFactory<TaleshavenDbContext> dbFactory, TimeProvider timeProvider) : IChronicleService
{
    public async Task<ChronicleView> GetPageAsync(int campaignId, int page, string viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var access = await CampaignAccess.GetAsync(db, campaignId, viewerId, cancellationToken);

        var contents = await db.ChronicleChapters.AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .OrderBy(c => c.Position)
            .Select(c => new ChapterSummary(c.Id, c.Position, c.Title))
            .ToListAsync(cancellationToken);

        var pageCount = ChronicleLimits.PageCount(contents.Count);
        page = Math.Clamp(page, 1, pageCount);

        var chapters = await (
                from c in db.ChronicleChapters.AsNoTracking()
                where c.CampaignId == campaignId
                join u in db.Users on c.AuthorId equals u.Id
                orderby c.Position
                select new ChapterView(c.Id, c.Position, c.Title, c.Content, u.DisplayName, c.CreatedAt, c.UpdatedAt))
            .Skip((page - 1) * ChronicleLimits.ChaptersPerPage)
            .Take(ChronicleLimits.ChaptersPerPage)
            .ToListAsync(cancellationToken);

        return new ChronicleView(contents, chapters, page, pageCount, CampaignPermissions.CanEditChronicle(access.Role));
    }

    public async Task<ChapterView?> GetChapterAsync(int campaignId, int chapterId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await (
                from c in db.ChronicleChapters.AsNoTracking()
                where c.CampaignId == campaignId && c.Id == chapterId
                join u in db.Users on c.AuthorId equals u.Id
                select new ChapterView(c.Id, c.Position, c.Title, c.Content, u.DisplayName, c.CreatedAt, c.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateChapterAsync(int campaignId, string userId, string? title, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanEditAsync(db, campaignId, userId, cancellationToken);

        var chapters = await LoadChaptersAsync(db, campaignId, cancellationToken);
        var chapter = ChronicleChapter.Create(campaignId, ChronicleOrdering.NextPosition(chapters), title, content, userId, timeProvider.GetUtcNow());

        db.ChronicleChapters.Add(chapter);
        await db.SaveChangesAsync(cancellationToken);
        return chapter.Position;
    }

    public async Task<int> UpdateChapterAsync(int campaignId, int chapterId, string userId, string? title, string? content, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanEditAsync(db, campaignId, userId, cancellationToken);

        var chapter = await db.ChronicleChapters.SingleOrDefaultAsync(c => c.CampaignId == campaignId && c.Id == chapterId, cancellationToken)
            ?? throw new CampaignRuleException("Kapitlet finns inte.");

        chapter.Update(title, content, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return chapter.Position;
    }

    public async Task DeleteChapterAsync(int campaignId, int chapterId, string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanEditAsync(db, campaignId, userId, cancellationToken);

        var chapters = await LoadChaptersAsync(db, campaignId, cancellationToken);
        var chapter = chapters.SingleOrDefault(c => c.Id == chapterId)
            ?? throw new CampaignRuleException("Kapitlet finns inte.");

        ChronicleOrdering.Remove(chapters, chapter);
        db.ChronicleChapters.Remove(chapter);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> MoveChapterAsync(int campaignId, int chapterId, string userId, int direction, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCanEditAsync(db, campaignId, userId, cancellationToken);

        var chapters = await LoadChaptersAsync(db, campaignId, cancellationToken);
        var chapter = chapters.SingleOrDefault(c => c.Id == chapterId)
            ?? throw new CampaignRuleException("Kapitlet finns inte.");

        ChronicleOrdering.Move(chapters, chapter, direction);
        await db.SaveChangesAsync(cancellationToken);
        return chapter.Position;
    }

    private static async Task EnsureCanEditAsync(TaleshavenDbContext db, int campaignId, string userId, CancellationToken cancellationToken)
    {
        var access = await CampaignAccess.GetAsync(db, campaignId, userId, cancellationToken);
        if (!CampaignPermissions.CanEditChronicle(access.Role))
            throw new CampaignRuleException("Endast kampanjens GM kan skriva i krönikan.");
    }

    private static async Task<List<ChronicleChapter>> LoadChaptersAsync(TaleshavenDbContext db, int campaignId, CancellationToken cancellationToken) =>
        await db.ChronicleChapters.Where(c => c.CampaignId == campaignId).ToListAsync(cancellationToken);
}
