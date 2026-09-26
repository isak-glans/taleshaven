using Microsoft.EntityFrameworkCore;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
using Taleshaven.Core.Site;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Portraits;

internal sealed class PortraitService(
    IDbContextFactory<TaleshavenDbContext> dbFactory,
    ISiteRoleService siteRoles,
    IImageStore imageStore,
    TimeProvider timeProvider) : IPortraitService
{
    public async Task<PortraitPage> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // Varje sökord ska matcha början av någon tagg, så "dvä kri" hittar #dvärg #krigare.
        var portraits = db.Portraits.AsNoTracking();
        foreach (var term in PortraitTags.ParseSearch(query))
            portraits = portraits.Where(p => p.Tags.Any(tag => tag.StartsWith(term)));

        var rows = await portraits
            .OrderByDescending(p => p.Id)
            .Take(limit + 1)
            .Select(p => new { p.Id, p.ImageKey, p.Tags, p.Source, UsageCount = db.Characters.Count(c => c.PortraitId == p.Id) })
            .ToListAsync(cancellationToken);

        return new PortraitPage(
            rows.Take(limit).Select(r => new PortraitView(r.Id, IImageStore.PortraitUrl(r.ImageKey), r.Tags, r.Source, r.UsageCount)).ToList(),
            HasMore: rows.Count > limit);
    }

    public async Task<IReadOnlyList<TagCount>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var tagLists = await db.Portraits.AsNoTracking().Select(p => p.Tags).ToListAsync(cancellationToken);
        return tagLists
            .SelectMany(tags => tags)
            .GroupBy(tag => tag)
            .Select(group => new TagCount(group.Key, group.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Tag, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<int> UploadAsync(string userId, byte[] image, string? tags, string? source, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(userId, cancellationToken);

        // Taggarna kontrolleras innan bilden sparas, så att ett skrivfel inte lämnar en fil utan porträtt.
        PortraitTags.Parse(tags);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var imageKey = await imageStore.SavePortraitAsync(image, cancellationToken);
        try
        {
            var portrait = Portrait.Create(imageKey, userId, tags, source, timeProvider.GetUtcNow());
            db.Portraits.Add(portrait);
            await db.SaveChangesAsync(cancellationToken);
            return portrait.Id;
        }
        catch
        {
            imageStore.DeletePortrait(imageKey);
            throw;
        }
    }

    public async Task UpdateAsync(string userId, int portraitId, string? tags, string? source, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(userId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var portrait = await LoadAsync(db, portraitId, cancellationToken);

        portrait.Update(tags, source);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string userId, int portraitId, CancellationToken cancellationToken = default)
    {
        await EnsureCanManageAsync(userId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var portrait = await LoadAsync(db, portraitId, cancellationToken);

        // Databasen nollställer PortraitId på karaktärerna (ON DELETE SET NULL), så de får initialer (B20).
        db.Portraits.Remove(portrait);
        await db.SaveChangesAsync(cancellationToken);

        imageStore.DeletePortrait(portrait.ImageKey);
    }

    private async Task EnsureCanManageAsync(string userId, CancellationToken cancellationToken)
    {
        if (!SitePermissions.CanManagePortraits(await siteRoles.GetRolesAsync(userId, cancellationToken)))
            throw new CampaignRuleException("Endast administratörer och managers kan hantera porträttbiblioteket.");
    }

    private static async Task<Portrait> LoadAsync(TaleshavenDbContext db, int portraitId, CancellationToken cancellationToken) =>
        await db.Portraits.SingleOrDefaultAsync(p => p.Id == portraitId, cancellationToken)
        ?? throw new CampaignRuleException("Porträttet finns inte.");
}
