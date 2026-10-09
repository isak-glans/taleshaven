using System.Text;

namespace Taleshaven.Core.Portraits;

/// <summary>
/// Ett manifest över bilder (B65), som CSV med kolumnerna <c>file,kind,tags,source</c>:
/// <code>
/// file,kind,tags,source
/// 0cd96cb8b2179047ac571d5071bd1364.webp,portrait,dwarf character man red-hair,"Dwarves, sheet 1"
/// </code>
/// Samma format används på två ställen: bredvid bibliotekets bilder (<c>media/images/manifest.csv</c>), där det skrivs
/// om efter varje ändring och används för att återställa databasen, och i inkorgen <c>assets/new_images/</c>, där det
/// anger taggarna för nya bilder. <c>kind</c> är <c>portrait</c> eller <c>icon</c>; taggarna skiljs åt med mellanslag.
/// </summary>
public static class ImageManifest
{
    public const string Header = "file,kind,tags,source";

    public static string Format(IEnumerable<ImageManifestRow> rows)
    {
        var text = new StringBuilder(Header).Append('\n');
        foreach (var row in rows)
        {
            text.Append(Field(row.File)).Append(',')
                .Append(KindName(row.Kind)).Append(',')
                .Append(Field(string.Join(' ', row.Tags))).Append(',')
                .Append(Field(row.Source ?? "")).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>
    /// Läser manifestet. Raden med kolumnnamn, tomma rader och rader som börjar med <c>#</c> hoppas över.
    /// Kastar <see cref="FormatException"/> med radnumret vid fel. Taggarna kontrolleras inte här.
    /// </summary>
    public static IReadOnlyList<ImageManifestRow> Parse(string text)
    {
        var rows = new List<ImageManifestRow>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#') || (rows.Count == 0 && line.Trim() == Header))
                continue;

            var fields = SplitFields(line, i + 1);
            if (fields.Count is < 3 or > 4)
                throw new FormatException($"Rad {i + 1}: väntade 3–4 fält (file,kind,tags,source), fick {fields.Count}.");
            var file = fields[0].Trim();
            if (file.Length == 0 || file.Contains('/') || file.Contains('\\'))
                throw new FormatException($"Rad {i + 1}: \"{file}\" är inget filnamn.");
            var kind = fields[1].Trim().ToLowerInvariant() switch
            {
                "portrait" => ImageKind.Portrait,
                "icon" => ImageKind.Icon,
                var other => throw new FormatException($"Rad {i + 1}: typen \"{other}\" är varken portrait eller icon."),
            };
            var tags = fields[2].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var source = fields.Count > 3 && fields[3].Trim().Length > 0 ? fields[3].Trim() : null;
            rows.Add(new ImageManifestRow(file, kind, tags, source));
        }
        return rows;
    }

    public static string KindName(ImageKind kind) => kind == ImageKind.Icon ? "icon" : "portrait";

    // Fält med komma, citattecken eller radbrytning citeras, och citattecken dubbleras (RFC 4180).
    private static string Field(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) < 0 ? value : $"\"{value.Replace("\"", "\"\"")}\"";

    private static List<string> SplitFields(string line, int number)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                    quoted = false;
                else
                    current.Append(c);
            }
            else if (c == '"' && current.ToString().Trim().Length == 0)
            {
                current.Clear();
                quoted = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(c);
        }
        if (quoted)
            throw new FormatException($"Rad {number}: ett citattecken stängs inte.");
        fields.Add(current.ToString());
        return fields;
    }
}

/// <summary>En bild i ett manifest: filnamn, typ, taggar och källa (kan saknas).</summary>
public sealed record ImageManifestRow(string File, ImageKind Kind, IReadOnlyList<string> Tags, string? Source)
{
    public bool Equals(ImageManifestRow? other) =>
        other is not null && File == other.File && Kind == other.Kind && Tags.SequenceEqual(other.Tags) && Source == other.Source;

    public override int GetHashCode() => HashCode.Combine(File, Kind, Source);
}
