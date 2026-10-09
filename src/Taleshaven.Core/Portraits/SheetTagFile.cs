namespace Taleshaven.Core.Portraits;

/// <summary>
/// Taggfilen till ett källark i <c>assets/sources/</c> (B64), t.ex. <c>dwarves_1.tags.txt</c> bredvid <c>dwarves_1.png</c>:
/// <code>
/// # Kommentar
/// source: Dwarves (assets/sources/portraits/dwarves_1.png)
/// 1.1: dwarf character man red-hair beard
/// 1.2:
/// </code>
/// Varje bild anges som "rad.kolumn: taggar". En position utan taggar hoppas över vid importen.
/// </summary>
public sealed record SheetTagFile(string? Source, IReadOnlyList<SheetTagEntry> Entries)
{
    public const string Extension = ".tags.txt";

    /// <summary>Mappen under <c>assets/sources/</c> (och <c>assets/</c> för de utklippta bilderna) för varje bildtyp.</summary>
    public static string Folder(ImageKind kind) => kind == ImageKind.Icon ? "icons" : "portraits";

    public static string ImportKey(ImageKind kind, string sheet, int row, int column) => $"{Folder(kind)}/{sheet}/{row}.{column}";

    public static SheetTagFile Parse(IEnumerable<string> lines)
    {
        string? source = null;
        var entries = new List<SheetTagEntry>();
        var seen = new HashSet<string>();
        var number = 0;
        foreach (var raw in lines)
        {
            number++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var colon = line.IndexOf(':');
            if (colon < 0)
                throw new FormatException($"Rad {number}: \"{line}\" saknar kolon.");
            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            if (key.Equals("source", StringComparison.OrdinalIgnoreCase))
            {
                source = value.Length == 0 ? null : value;
                continue;
            }

            var parts = key.Split('.');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var row) || !int.TryParse(parts[1], out var column) || row < 1 || column < 1)
                throw new FormatException($"Rad {number}: \"{key}\" är inte en position som 1.2 (rad.kolumn).");
            var position = $"{row}.{column}";
            if (!seen.Add(position))
                throw new FormatException($"Rad {number}: positionen {position} finns redan.");
            entries.Add(new SheetTagEntry(row, column, value));
        }
        return new SheetTagFile(source, entries);
    }

    /// <summary>Taggfilen som text, med en tom rad för varje position som saknar taggar.</summary>
    public static IEnumerable<string> Template(string source, IEnumerable<(int Row, int Column)> positions) =>
        new[] { "# Taggar per bild: \"rad.kolumn: taggar\". Positioner utan taggar läggs inte in. Se B64 i projektbeskrivningen.", $"source: {source}" }
            .Concat(positions.Select(p => $"{p.Row}.{p.Column}: "));
}

/// <summary>En bild i ett källark: position och taggar (kan vara tomt).</summary>
public sealed record SheetTagEntry(int Row, int Column, string Tags)
{
    public string Position => $"{Row}.{Column}";

    /// <summary>Bildens importnyckel, t.ex. "portraits/dwarves_1/7.8" (se <see cref="Portrait.ImportKey"/>).</summary>
    public string ImportKey(ImageKind kind, string sheet) => SheetTagFile.ImportKey(kind, sheet, Row, Column);
}
