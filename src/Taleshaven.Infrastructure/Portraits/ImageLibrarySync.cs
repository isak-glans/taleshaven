using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Media;

namespace Taleshaven.Infrastructure.Portraits;

/// <summary>
/// Håller bildbiblioteket och dess manifest i takt (B65). Körs när appen startar:
/// <list type="number">
/// <item>Bilder som står i <c>media/images/manifest.csv</c> och finns som fil men saknas i databasen läggs till, så att
/// en ny databas kan fyllas i från bildmappen (t.ex. i produktion).</item>
/// <item>Bilder som saknar kontrollsumma får en.</item>
/// <item>I utvecklingsläge klipps först varje ark i <c>assets/new_images/sheets/</c> ut till inkorgen (256×256 WebP,
/// <c>&lt;ark&gt;_&lt;rad&gt;-&lt;kolumn&gt;.webp</c>, och ett numrerat översiktsark), och arket flyttas till
/// <c>assets/sources/</c>. Utklippen saknar rad i manifestet och ligger kvar tills de taggats.</item>
/// <item>I utvecklingsläge läses inkorgen <c>assets/new_images/</c>: varje bild med en rad i inkorgens
/// <c>manifest.csv</c> sparas (om samma bild inte redan finns) och tas sedan bort ur inkorgen. När inkorgen är tom tas
/// även dess manifest och översiktsark bort. Bilder utan rad eller med ogiltiga taggar ligger kvar och nämns i loggen.</item>
/// <item>Manifestet skrivs om från databasen, som efter varje ändring i biblioteket, men bara i utvecklingsmiljön
/// (<see cref="ImageLibraryOptions.WriteManifest"/>); i produktion läses det bara.</item>
/// </list>
/// </summary>
public static class ImageLibrarySync
{
    public const string InboxManifestFileName = "manifest.csv";
    public const string SheetsFolder = "sheets";
    private const string OverviewSuffix = ".overview.png";

    private static readonly string[] InboxExtensions = [".webp", ".png", ".jpg", ".jpeg"];
    private static readonly SemaphoreSlim ManifestLock = new(1, 1);

    public static async Task RunAsync(IServiceProvider services, string? inboxPath, IReadOnlyList<string> adminEmails,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var dbFactory = provider.GetRequiredService<IDbContextFactory<TaleshavenDbContext>>();
        var store = provider.GetRequiredService<IImageStore>();
        var processor = provider.GetRequiredService<IImageProcessor>();
        var now = provider.GetRequiredService<TimeProvider>().GetUtcNow();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(ImageLibrarySync));
        var options = provider.GetRequiredService<ImageLibraryOptions>();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await RestoreFromManifestAsync(db, store, now, logger, cancellationToken);
        await AddMissingHashesAsync(db, store, logger, cancellationToken);
        if (inboxPath is not null && Directory.Exists(inboxPath))
        {
            CutSheets(inboxPath, logger);
            await ProcessInboxAsync(db, store, processor, inboxPath, adminEmails, now, logger, cancellationToken);
        }
        await WriteManifestAsync(db, store, options, cancellationToken);
    }

    /// <summary>
    /// Skriver om bibliotekets manifest från databasen. Anropas efter varje ändring i biblioteket; gör ingenting utanför
    /// utvecklingsmiljön, så att flera servrar aldrig skriver samma fil.
    /// </summary>
    public static async Task WriteManifestAsync(TaleshavenDbContext db, IImageStore store, ImageLibraryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.WriteManifest)
            return;
        await ManifestLock.WaitAsync(cancellationToken);
        try
        {
            var rows = await db.Portraits.AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new ImageManifestRow(p.ImageKey, p.Kind, p.Tags, p.Source))
                .ToListAsync(cancellationToken);
            await store.WriteManifestAsync(ImageManifest.Format(rows), cancellationToken);
        }
        finally
        {
            ManifestLock.Release();
        }
    }

    public static string Hash(byte[] image) => Convert.ToHexStringLower(SHA256.HashData(image));

    private static async Task RestoreFromManifestAsync(TaleshavenDbContext db, IImageStore store, DateTimeOffset now, ILogger logger,
        CancellationToken cancellationToken)
    {
        var text = await store.ReadManifestAsync(cancellationToken);
        if (text is null)
            return;

        IReadOnlyList<ImageManifestRow> rows;
        try
        {
            rows = ImageManifest.Parse(text);
        }
        catch (FormatException ex)
        {
            logger.LogError("Bildbibliotekets manifest kunde inte läsas och har inte använts: {Error}", ex.Message);
            return;
        }

        var existing = (await db.Portraits.Select(p => p.ImageKey).ToListAsync(cancellationToken)).ToHashSet();
        var added = 0;
        foreach (var row in rows.Where(r => !existing.Contains(r.File)))
        {
            var image = await ReadAsync(store, row.File, cancellationToken);
            if (image is null)
            {
                logger.LogWarning("Bilden {File} står i manifestet men finns inte i bildmappen.", row.File);
                continue;
            }
            try
            {
                db.Portraits.Add(Portrait.Create(row.File, null, row.Kind, string.Join(' ', row.Tags), row.Source, now, Hash(image)));
                added++;
            }
            catch (CampaignRuleException ex)
            {
                logger.LogWarning("Bilden {File} i manifestet har ogiltiga uppgifter: {Error}", row.File, ex.Message);
            }
        }
        if (added == 0)
            return;
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        logger.LogInformation("{Count} bilder lades till i databasen från bildbibliotekets manifest.", added);
    }

    private static async Task AddMissingHashesAsync(TaleshavenDbContext db, IImageStore store, ILogger logger, CancellationToken cancellationToken)
    {
        var missing = await db.Portraits.Where(p => p.ContentHash == null).ToListAsync(cancellationToken);
        foreach (var portrait in missing)
        {
            if (await ReadAsync(store, portrait.ImageKey, cancellationToken) is { } image)
                portrait.SetContentHash(Hash(image));
            else
                logger.LogWarning("Bilden {File} (id {Id}) saknas i bildmappen.", portrait.ImageKey, portrait.Id);
        }
        if (missing.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static async Task ProcessInboxAsync(TaleshavenDbContext db, IImageStore store, IImageProcessor processor, string inboxPath,
        IReadOnlyList<string> adminEmails, DateTimeOffset now, ILogger logger, CancellationToken cancellationToken)
    {
        var images = Directory.GetFiles(inboxPath)
            .Where(f => InboxExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()) && !f.EndsWith(OverviewSuffix, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToList();
        var manifestPath = Path.Combine(inboxPath, InboxManifestFileName);
        if (images.Count == 0)
        {
            ClearInbox(inboxPath);
            return;
        }
        if (!File.Exists(manifestPath))
        {
            logger.LogWarning("Inkorgen {Path} har {Count} bilder men inget {Manifest}; inget har lagts in.", inboxPath, images.Count, InboxManifestFileName);
            return;
        }

        Dictionary<string, ImageManifestRow> rows;
        try
        {
            rows = ImageManifest.Parse(await File.ReadAllTextAsync(manifestPath, cancellationToken))
                .ToDictionary(r => r.File, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            logger.LogError("Inkorgens manifest kunde inte läsas: {Error}", ex.Message);
            return;
        }

        var uploaderId = await FindUploaderAsync(db, adminEmails, cancellationToken);
        int added = 0, duplicates = 0, left = 0;
        foreach (var path in images)
        {
            var file = Path.GetFileName(path);
            if (!rows.TryGetValue(file, out var row))
            {
                logger.LogWarning("Inkorgen: {File} saknar rad i manifestet och ligger kvar.", file);
                left++;
                continue;
            }

            string? savedKey = null;
            try
            {
                var tags = string.Join(' ', row.Tags);
                PortraitTags.Parse(tags);
                byte[] image;
                await using (var source = File.OpenRead(path))
                    image = processor.CreatePortrait(source);
                var hash = Hash(image);

                if (await db.Portraits.AnyAsync(p => p.ContentHash == hash, cancellationToken))
                {
                    duplicates++;
                }
                else
                {
                    savedKey = await store.SavePortraitAsync(image, cancellationToken);
                    db.Portraits.Add(Portrait.Create(savedKey, uploaderId, row.Kind, tags, row.Source, now, hash));
                    await db.SaveChangesAsync(cancellationToken);
                    added++;
                }
                File.Delete(path);
            }
            catch (CampaignRuleException ex)
            {
                if (savedKey is not null)
                    store.DeletePortrait(savedKey);
                db.ChangeTracker.Clear();
                logger.LogWarning("Inkorgen: {File} ligger kvar: {Error}", file, ex.Message);
                left++;
            }
        }

        if (left == 0)
            ClearInbox(inboxPath);
        logger.LogInformation("Inkorgen: {Added} nya bilder, {Duplicates} fanns redan, {Left} ligger kvar.", added, duplicates, left);
    }

    // Manifestet och översiktsarken behövs inte när alla bilder i inkorgen är behandlade.
    private static void ClearInbox(string inboxPath)
    {
        File.Delete(Path.Combine(inboxPath, InboxManifestFileName));
        foreach (var overview in Directory.GetFiles(inboxPath, "*" + OverviewSuffix))
            File.Delete(overview);
    }

    private static void CutSheets(string inboxPath, ILogger logger)
    {
        var sheetsPath = Path.Combine(inboxPath, SheetsFolder);
        if (!Directory.Exists(sheetsPath))
            return;
        var sources = Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(inboxPath))!, "sources");

        foreach (var sheetPath in Directory.GetFiles(sheetsPath)
                     .Where(f => InboxExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                     .Order(StringComparer.Ordinal))
        {
            var sheet = Path.GetFileNameWithoutExtension(sheetPath);
            try
            {
                var tiles = SheetCutter.Cut(sheetPath);
                foreach (var tile in tiles)
                    File.WriteAllBytes(Path.Combine(inboxPath, $"{sheet}_{tile.Row}-{tile.Column}.webp"), tile.Image);
                File.WriteAllBytes(Path.Combine(inboxPath, sheet + OverviewSuffix), SheetCutter.Overview(tiles));

                // Arket sparas som råmaterial; finns ett med samma namn får det nya ett nummer.
                Directory.CreateDirectory(sources);
                var target = Path.Combine(sources, Path.GetFileName(sheetPath));
                for (var n = 2; File.Exists(target); n++)
                    target = Path.Combine(sources, $"{sheet}_{n}{Path.GetExtension(sheetPath)}");
                File.Move(sheetPath, target);
                logger.LogInformation("Inkorgen: arket {Sheet} gav {Count} bilder; tagga dem i {Manifest}.", sheet, tiles.Count, InboxManifestFileName);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                logger.LogWarning("Inkorgen: arket {Sheet} kunde inte klippas ut och ligger kvar: {Error}", sheet, ex.Message);
            }
        }
    }

    private static async Task<byte[]?> ReadAsync(IImageStore store, string key, CancellationToken cancellationToken)
    {
        await using var stream = store.OpenPortrait(key);
        if (stream is null)
            return null;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    // Bilderna från inkorgen sparas i den första administratörens namn (Admin:Emails), annars utan uppladdare.
    private static async Task<string?> FindUploaderAsync(TaleshavenDbContext db, IReadOnlyList<string> emails, CancellationToken cancellationToken)
    {
        foreach (var email in emails)
        {
            var normalized = email.Trim().ToUpperInvariant();
            var id = await db.Users.Where(u => u.NormalizedEmail == normalized).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            if (id is not null)
                return id;
        }
        return null;
    }
}

/// <summary>Inställningar för bildbiblioteket (B65).</summary>
/// <param name="WriteManifest">Om manifestet skrivs om efter ändringar; sant bara i utvecklingsmiljön.</param>
public sealed record ImageLibraryOptions(bool WriteManifest);
