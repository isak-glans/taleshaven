using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Taleshaven.Core;
using Taleshaven.Core.Media;
using Taleshaven.Core.Portraits;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Media;

namespace Taleshaven.Infrastructure.Portraits;

/// <summary>
/// Lägger in bilder ur källarken i <c>assets/sources/</c> i bildbiblioteket (B64). Körs från kommandoraden:
/// <code>
/// dotnet run --project src/Taleshaven.Web -- images preview assets/sources/portraits/dwarves_1.png
/// dotnet run --project src/Taleshaven.Web -- images import [--dry-run] [--as admin@exempel.se]
/// </code>
/// <c>preview</c> klipper ut arket, sparar ett numrerat översiktsark och skapar en tom taggfil att fylla i.
/// <c>import</c> läser alla taggfiler: nya bilder klipps ut och läggs till, bilder som redan finns (samma
/// <see cref="Portrait.ImportKey"/>) får taggar, typ och källa från taggfilen. Inget tas bort; bilder som finns i
/// biblioteket men inte i taggfilen nämns bara. Typen följer mappen: <c>sources/portraits</c> eller <c>sources/icons</c>.
/// </summary>
public static class ImageLibraryImport
{
    private const string SourcesFolder = "sources";

    public static async Task<int> RunAsync(IServiceProvider services, IReadOnlyList<string> args, IReadOnlyList<string> adminEmails,
        TextWriter output, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (args.FirstOrDefault())
            {
                case "preview" when args.Count >= 2:
                    Preview(args[1], output);
                    return 0;
                case "import":
                    var options = args.Skip(1).ToList();
                    var dryRun = options.Remove("--dry-run");
                    var asIndex = options.IndexOf("--as");
                    var uploader = asIndex >= 0 && asIndex + 1 < options.Count ? [options[asIndex + 1]] : adminEmails;
                    await using (var scope = services.CreateAsyncScope())
                    {
                        var provider = scope.ServiceProvider;
                        return await ImportAsync(provider.GetRequiredService<IDbContextFactory<TaleshavenDbContext>>(),
                            provider.GetRequiredService<IImageStore>(), provider.GetRequiredService<TimeProvider>(),
                            FindAssets(), uploader, dryRun, output, cancellationToken);
                    }
                default:
                    output.WriteLine("Användning: images preview <ark.png> | images import [--dry-run] [--as <e-post>]");
                    return 2;
            }
        }
        catch (Exception ex) when (ex is IOException or FormatException or InvalidOperationException)
        {
            output.WriteLine($"Fel: {ex.Message}");
            return 1;
        }
    }

    private static void Preview(string sheetPath, TextWriter output)
    {
        var sheet = Path.GetFileNameWithoutExtension(sheetPath);
        var folder = Path.GetDirectoryName(Path.GetFullPath(sheetPath))!;
        var kind = Path.GetFileName(folder) == SheetTagFile.Folder(ImageKind.Icon) ? ImageKind.Icon : ImageKind.Portrait;

        var tiles = SheetCutter.Cut(sheetPath);
        var overview = Path.Combine(folder, $"{sheet}.overview.png");
        File.WriteAllBytes(overview, SheetCutter.Overview(tiles));
        output.WriteLine($"{tiles.Count} bilder i {tiles.Max(t => t.Row)} rader och {tiles.Max(t => t.Column)} kolumner. Översikt: {overview}");

        // Utklippen läggs i assets/<typ>/<ark>/ om arket ligger i assets/sources/<typ>/.
        if (Path.GetFileName(Path.GetDirectoryName(folder)) == SourcesFolder)
            WriteTiles(Path.GetDirectoryName(Path.GetDirectoryName(folder))!, kind, sheet, tiles);

        var tagPath = Path.Combine(folder, sheet + SheetTagFile.Extension);
        if (File.Exists(tagPath))
        {
            output.WriteLine($"Taggfilen finns redan: {tagPath}");
            return;
        }
        File.WriteAllLines(tagPath, SheetTagFile.Template(sheet, tiles.Select(t => (t.Row, t.Column))));
        output.WriteLine($"Fyll i taggarna i {tagPath} och kör sedan: images import");
    }

    private static async Task<int> ImportAsync(IDbContextFactory<TaleshavenDbContext> dbFactory, IImageStore store, TimeProvider time,
        string assets, IReadOnlyList<string> uploaderEmails, bool dryRun, TextWriter output, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        string? uploaderId = null;
        int added = 0, updated = 0, unchanged = 0, problems = 0;

        foreach (var kind in Enum.GetValues<ImageKind>())
        {
            var folder = Path.Combine(assets, SourcesFolder, SheetTagFile.Folder(kind));
            if (!Directory.Exists(folder))
                continue;

            foreach (var tagPath in Directory.GetFiles(folder, "*" + SheetTagFile.Extension).Order(StringComparer.Ordinal))
            {
                var sheet = Path.GetFileName(tagPath)[..^SheetTagFile.Extension.Length];
                var file = SheetTagFile.Parse(await File.ReadAllLinesAsync(tagPath, cancellationToken));
                var prefix = $"{SheetTagFile.Folder(kind)}/{sheet}/";
                var existing = await db.Portraits
                    .Where(p => p.ImportKey != null && p.ImportKey.StartsWith(prefix))
                    .ToDictionaryAsync(p => p.ImportKey!, cancellationToken);
                IReadOnlyList<SheetTile>? tiles = null;
                var savedKeys = new List<string>();
                int sheetAdded = 0, sheetUpdated = 0;

                foreach (var entry in file.Entries.Where(e => e.Tags.Length > 0))
                {
                    var key = entry.ImportKey(kind, sheet);
                    try
                    {
                        if (existing.Remove(key, out var portrait))
                        {
                            if (portrait.Kind == kind && portrait.Source == file.Source
                                && portrait.Tags.SequenceEqual(PortraitTags.Parse(entry.Tags)))
                            {
                                unchanged++;
                                continue;
                            }
                            portrait.Update(kind, entry.Tags, file.Source);
                            sheetUpdated++;
                            continue;
                        }

                        PortraitTags.Parse(entry.Tags);
                        if (tiles is null)
                        {
                            var sheetPath = Path.Combine(folder, sheet + ".png");
                            if (!File.Exists(sheetPath))
                            {
                                output.WriteLine($"  {sheet}: arket {sheetPath} saknas, så nya bilder kan inte klippas ut.");
                                problems++;
                                break;
                            }
                            tiles = SheetCutter.Cut(sheetPath);
                            if (!dryRun)
                                WriteTiles(assets, kind, sheet, tiles);
                        }
                        var tile = tiles.FirstOrDefault(t => t.Row == entry.Row && t.Column == entry.Column);
                        if (tile is null)
                        {
                            output.WriteLine($"  {key}: ingen bild på den positionen i arket.");
                            problems++;
                            continue;
                        }

                        uploaderId ??= await FindUploaderAsync(db, uploaderEmails, cancellationToken);
                        var imageKey = dryRun ? "dry-run" : await store.SavePortraitAsync(tile.Image, cancellationToken);
                        if (!dryRun)
                            savedKeys.Add(imageKey);
                        db.Portraits.Add(Portrait.Create(imageKey, uploaderId, kind, entry.Tags, file.Source, time.GetUtcNow(), key));
                        sheetAdded++;
                    }
                    catch (CampaignRuleException ex)
                    {
                        output.WriteLine($"  {key}: {ex.Message}");
                        problems++;
                    }
                }

                foreach (var key in existing.Keys.Order(StringComparer.Ordinal))
                    output.WriteLine($"  {key}: finns i biblioteket men saknar taggar i taggfilen; lämnas som den är.");

                if (!dryRun && (sheetAdded > 0 || sheetUpdated > 0))
                {
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch
                    {
                        foreach (var saved in savedKeys)
                            store.DeletePortrait(saved);
                        throw;
                    }
                }
                db.ChangeTracker.Clear();
                added += sheetAdded;
                updated += sheetUpdated;
                output.WriteLine($"{SheetTagFile.Folder(kind)}/{sheet}: {sheetAdded} nya, {sheetUpdated} ändrade");
            }
        }

        output.WriteLine($"{(dryRun ? "Provkörning, inget sparat. " : "")}Totalt {added} nya, {updated} ändrade, {unchanged} oförändrade, {problems} problem.");
        return problems == 0 ? 0 : 1;
    }

    // Bilderna sparas i den första administratörens namn (Admin:Emails), eller den som anges med --as.
    private static async Task<string> FindUploaderAsync(TaleshavenDbContext db, IReadOnlyList<string> emails, CancellationToken cancellationToken)
    {
        foreach (var email in emails)
        {
            var normalized = email.Trim().ToUpperInvariant();
            var id = await db.Users.Where(u => u.NormalizedEmail == normalized).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            if (id is not null)
                return id;
        }
        throw new InvalidOperationException("Hittar ingen användare att lägga in bilderna som. Ange en med --as <e-post>.");
    }

    private static void WriteTiles(string assets, ImageKind kind, string sheet, IReadOnlyList<SheetTile> tiles)
    {
        var folder = Path.Combine(assets, SheetTagFile.Folder(kind), sheet);
        Directory.CreateDirectory(folder);
        foreach (var tile in tiles)
            File.WriteAllBytes(Path.Combine(folder, $"{tile.Position}.webp"), tile.Image);
    }

    // assets-mappen i repots rot, sökt uppåt från den aktuella mappen (dotnet run kör i projektmappen).
    private static string FindAssets()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "assets", SourcesFolder);
            if (Directory.Exists(candidate))
                return Path.Combine(dir.FullName, "assets");
        }
        throw new InvalidOperationException("Hittar ingen mapp assets/sources ovanför den aktuella mappen.");
    }
}
