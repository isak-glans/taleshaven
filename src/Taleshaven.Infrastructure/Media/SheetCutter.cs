using SkiaSharp;
using Taleshaven.Core.Media;

namespace Taleshaven.Infrastructure.Media;

/// <summary>En utklippt bild ur ett källark (B64): position och färdig 256×256 WebP med genomskinliga hörn.</summary>
public sealed record SheetTile(int Row, int Column, byte[] Image)
{
    public string Position => $"{Row}.{Column}";
}

/// <summary>
/// Klipper ut de runda bilderna ur ett källark (B53, B64). Allt som skiljer sig från den ljusa bakgrunden delas i
/// sammanhängande områden; de tydliga cirklarna ger rutnätets rader och kolumner, och varje ruta klipps ut som en rund
/// bild. En rad som klipps av underkanten tas med om nästan hela cirkeln syns, annars hoppas den över.
/// </summary>
public static class SheetCutter
{
    private const int WebpQuality = 85;
    private const int OverviewCell = 140;

    public static IReadOnlyList<SheetTile> Cut(string sheetPath)
    {
        using var bitmap = SKBitmap.Decode(sheetPath)
            ?? throw new InvalidOperationException($"{sheetPath} kunde inte läsas som bild.");
        var circles = FindCircles(bitmap);
        using var source = SKImage.FromBitmap(bitmap);
        return circles.Select(c => new SheetTile(c.Row, c.Column, Crop(source, c.X, c.Y, c.Radius))).ToList();
    }

    /// <summary>Ett numrerat översiktsark (PNG) att tagga efter: varje bild med sin position "rad.kolumn".</summary>
    public static byte[] Overview(IReadOnlyList<SheetTile> tiles)
    {
        if (tiles.Count == 0)
            throw new ArgumentException("Inga bilder.", nameof(tiles));
        var columns = tiles.Max(t => t.Column);
        var rows = tiles.Max(t => t.Row);
        using var sheet = SKSurface.Create(new SKImageInfo(columns * OverviewCell, rows * OverviewCell));
        sheet.Canvas.Clear(SKColors.White);
        using var font = new SKFont(SKTypeface.Default, 22);
        using var label = new SKPaint { Color = SKColors.Red, IsAntialias = true };
        foreach (var tile in tiles)
        {
            using var image = SKImage.FromEncodedData(tile.Image);
            float left = (tile.Column - 1) * OverviewCell, top = (tile.Row - 1) * OverviewCell;
            sheet.Canvas.DrawImage(image, SKRect.Create(left + 6, top + 6, 128, 128), new SKSamplingOptions(SKFilterMode.Linear));
            sheet.Canvas.DrawText(tile.Position, left + 4, top + 24, SKTextAlign.Left, font, label);
        }
        using var snapshot = sheet.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    private static byte[] Crop(SKImage source, float cx, float cy, float radius)
    {
        var size = ImageLimits.PortraitSize;
        // Lite innanför kanten, så att kantutjämningen mot bakgrunden inte följer med.
        var r = radius - 4f;
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using (var clip = new SKRoundRect(SKRect.Create(0, 0, size, size), size / 2f))
            surface.Canvas.ClipRoundRect(clip, antialias: true);
        surface.Canvas.DrawImage(source, SKRect.Create(cx - r, cy - r, 2 * r, 2 * r), SKRect.Create(0, 0, size, size),
            new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        return data.ToArray();
    }

    private static List<(int Row, int Column, float X, float Y, float Radius)> FindCircles(SKBitmap bitmap)
    {
        int w = bitmap.Width, h = bitmap.Height;
        var bg = bitmap.GetPixel(4, 4);

        var fg = new bool[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var c = bitmap.GetPixel(x, y);
            fg[y * w + x] = Math.Abs(c.Red - bg.Red) + Math.Abs(c.Green - bg.Green) + Math.Abs(c.Blue - bg.Blue) > 45;
        }

        // Tydliga cirklar: sammanhängande, ungefär kvadratiska och fyllda områden som inte klipps av kanten.
        var seen = new bool[w * h];
        var circles = new List<(int MinX, int MinY, int MaxX, int MaxY)>();
        var stack = new Stack<int>();
        for (var start = 0; start < fg.Length; start++)
        {
            if (!fg[start] || seen[start])
                continue;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0, count = 0;
            stack.Push(start);
            seen[start] = true;
            while (stack.Count > 0)
            {
                var i = stack.Pop();
                int x = i % w, y = i / w;
                count++;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                foreach (var n in new[] { i - 1, i + 1, i - w, i + w })
                {
                    if (n < 0 || n >= fg.Length || seen[n] || !fg[n]) continue;
                    if ((n == i - 1 && x == 0) || (n == i + 1 && x == w - 1)) continue;
                    seen[n] = true;
                    stack.Push(n);
                }
            }

            int bw = maxX - minX + 1, bh = maxY - minY + 1;
            var touchesEdge = minX <= 1 || minY <= 1 || maxX >= w - 2 || maxY >= h - 2;
            var squarish = Math.Abs(bw - bh) <= Math.Max(bw, bh) * 0.08;
            var filled = count > bw * bh * 0.6;
            if (bw >= 100 && bw <= 260 && squarish && filled && !touchesEdge)
                circles.Add((minX, minY, maxX, maxY));
        }
        if (circles.Count == 0)
            throw new InvalidOperationException("Hittade inga runda bilder i arket.");

        var diameter = circles.Select(c => c.MaxX - c.MinX + 1).Order().ElementAt(circles.Count / 2);
        var half = diameter / 2;
        var rowCenters = Cluster(circles.Select(c => (c.MinY + c.MaxY) / 2f), half);
        var colCenters = Cluster(circles.Select(c => (c.MinX + c.MaxX) / 2f), half);

        // En rad som klipps av underkanten syns inte bland de tydliga cirklarna; den läggs till om minst 80 % av en
        // cirkel får plats under den sista hela raden.
        if (rowCenters.Count >= 2)
        {
            var step = rowCenters[^1] - rowCenters[^2];
            var next = rowCenters[^1] + step;
            if (next - half < h && h - (next - half) >= diameter * 0.8)
                rowCenters.Add(next);
        }

        var result = new List<(int Row, int Column, float X, float Y, float Radius)>();
        for (var ri = 0; ri < rowCenters.Count; ri++)
        for (var ci = 0; ci < colCenters.Count; ci++)
        {
            int cx = (int)colCenters[ci], cy = (int)rowCenters[ri];
            if (cx - half < 0 || cy - half < 0 || cx + half >= w)
                continue;

            // Tom ruta (ingen cirkel): mest bakgrund i mitten.
            int hits = 0, samples = 0;
            for (var dy = -half / 2; dy <= half / 2; dy += 4)
            for (var dx = -half / 2; dx <= half / 2; dx += 4)
            {
                if (cy + dy >= h) continue;
                samples++;
                if (fg[(cy + dy) * w + cx + dx]) hits++;
            }
            if (samples == 0 || hits < samples * 0.5)
                continue;

            var found = circles.FirstOrDefault(c => Math.Abs((c.MinX + c.MaxX) / 2 - cx) < half / 2 && Math.Abs((c.MinY + c.MaxY) / 2 - cy) < half / 2);
            if (found != default)
            {
                result.Add((ri + 1, ci + 1, (found.MinX + found.MaxX) / 2f, (found.MinY + found.MaxY) / 2f,
                    Math.Max(found.MaxX - found.MinX, found.MaxY - found.MinY) / 2f));
                continue;
            }

            // Cirkeln flöt ihop med en granne (oftast raden under). Kolumnerna har mellanrum, så bredden mäts vågrätt
            // genom mitten, och överkanten uppåt; mitten och radien räknas fram ur dem.
            int Run(int x0, int y0, int dx, int dy)
            {
                int x = x0, y = y0, gap = 0, last = 0;
                for (var d = 1; d < diameter; d++)
                {
                    x += dx; y += dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) break;
                    if (fg[y * w + x]) { last = d; gap = 0; }
                    else if (++gap >= 3) break;
                }
                return last;
            }
            var left = Run(cx, cy, -1, 0);
            var right = Run(cx, cy, 1, 0);
            var radius = (left + right) / 2f;
            var centerX = cx + (right - left) / 2f;
            // Ljusa motiv (t.ex. kristaller) kan likna bakgrunden och ge för korta mätningar; då gäller rutnätet.
            if (radius < half * 0.8)
            {
                radius = half;
                centerX = cx;
            }
            var top = cy - Run((int)centerX, cy, 0, -1);
            var centerY = top + radius;
            if (Math.Abs(centerY - cy) > half * 0.25)
                centerY = cy;
            // Klipps cirkeln av underkanten blir utsnittet mindre, så att den avklippta kanten inte syns.
            radius = Math.Min(radius, h - 1 - centerY);
            result.Add((ri + 1, ci + 1, centerX, centerY, radius));
        }
        return result;
    }

    private static List<float> Cluster(IEnumerable<float> values, float gap)
    {
        var groups = new List<List<float>>();
        foreach (var v in values.Order())
        {
            if (groups.Count == 0 || v - groups[^1][^1] > gap) groups.Add([]);
            groups[^1].Add(v);
        }
        return groups.Select(g => g.Average()).ToList();
    }
}
