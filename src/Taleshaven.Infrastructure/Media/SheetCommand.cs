namespace Taleshaven.Infrastructure.Media;

/// <summary>
/// Klipper ut ett källark med många runda bilder (B53, B65), som förberedelse för inkorgen <c>assets/new_images/</c>:
/// <code>
/// dotnet run --project src/Taleshaven.Web -- images cut &lt;ark.png&gt; &lt;målmapp&gt;
/// </code>
/// Bilderna sparas som <c>&lt;ark&gt;_&lt;rad&gt;-&lt;kolumn&gt;.webp</c> i målmappen, tillsammans med ett numrerat översiktsark
/// <c>&lt;ark&gt;.overview.png</c>. Därefter skrivs inkorgens manifest med taggar per bild.
/// </summary>
public static class SheetCommand
{
    public static int Run(IReadOnlyList<string> args, TextWriter output)
    {
        if (args is not ["cut", var sheetPath, var target])
        {
            output.WriteLine("Användning: images cut <ark.png> <målmapp>");
            return 2;
        }

        try
        {
            var sheet = Path.GetFileNameWithoutExtension(sheetPath);
            var tiles = SheetCutter.Cut(sheetPath);
            Directory.CreateDirectory(target);
            foreach (var tile in tiles)
                File.WriteAllBytes(Path.Combine(target, $"{sheet}_{tile.Row}-{tile.Column}.webp"), tile.Image);
            var overview = Path.Combine(target, $"{sheet}.overview.png");
            File.WriteAllBytes(overview, SheetCutter.Overview(tiles));
            output.WriteLine($"{tiles.Count} bilder i {tiles.Max(t => t.Row)} rader och {tiles.Max(t => t.Column)} kolumner, sparade i {target}. Översikt: {overview}");
            return 0;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            output.WriteLine($"Fel: {ex.Message}");
            return 1;
        }
    }
}
