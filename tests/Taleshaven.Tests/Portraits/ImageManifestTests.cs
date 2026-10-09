using Taleshaven.Core.Portraits;

namespace Taleshaven.Tests.Portraits;

/// <summary>Bildbibliotekets och inkorgens manifest (B65).</summary>
public class ImageManifestTests
{
    [Fact]
    public void FormatAndParse_RoundTrip()
    {
        ImageManifestRow[] rows =
        [
            new("a.webp", ImageKind.Portrait, ["dwarf", "man", "red-hair"], "Dwarves, sheet 1"),
            new("b.webp", ImageKind.Icon, ["hp", "health"], null),
            new("c.webp", ImageKind.Portrait, ["elf"], "Says \"hello\""),
        ];

        var text = ImageManifest.Format(rows);

        Assert.StartsWith("file,kind,tags,source\n", text);
        Assert.Contains("a.webp,portrait,dwarf man red-hair,\"Dwarves, sheet 1\"\n", text);
        Assert.Contains("b.webp,icon,hp health,\n", text);
        Assert.Equal(rows, ImageManifest.Parse(text));
    }

    [Fact]
    public void Parse_SkipsCommentsAndBlankLinesAndAllowsMissingSource()
    {
        var rows = ImageManifest.Parse("# Nya dvärgar\r\nfile,kind,tags,source\r\n\r\ndwarves_2_1-1.webp, Portrait ,dwarf  man\r\n");

        Assert.Equal([new ImageManifestRow("dwarves_2_1-1.webp", ImageKind.Portrait, ["dwarf", "man"], null)], rows);
    }

    [Theory]
    [InlineData("a.webp,portrait")]
    [InlineData("a.webp,banner,dwarf")]
    [InlineData("../a.webp,portrait,dwarf")]
    [InlineData("a.webp,portrait,dwarf,\"unclosed")]
    public void Parse_RejectsBadRows(string line)
    {
        var error = Assert.Throws<FormatException>(() => ImageManifest.Parse("file,kind,tags,source\n" + line));
        Assert.StartsWith("Rad 2:", error.Message);
    }
}
