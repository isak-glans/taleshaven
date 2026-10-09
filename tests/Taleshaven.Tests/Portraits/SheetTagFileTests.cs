using Taleshaven.Core.Portraits;

namespace Taleshaven.Tests.Portraits;

/// <summary>Taggfilerna till källarken (B64) och taggknapparna i porträttväljarna (B62).</summary>
public class SheetTagFileTests
{
    [Fact]
    public void Parse_ReadsSourceAndPositions()
    {
        var file = SheetTagFile.Parse(
        [
            "# Kommentar",
            "source: Dwarves (assets/sources/portraits/dwarves_1.png)",
            "",
            "1.1: dwarf character man",
            " 1.2 :   ",
            "10.3: dwarf woman",
        ]);

        Assert.Equal("Dwarves (assets/sources/portraits/dwarves_1.png)", file.Source);
        Assert.Equal(
            [new SheetTagEntry(1, 1, "dwarf character man"), new SheetTagEntry(1, 2, ""), new SheetTagEntry(10, 3, "dwarf woman")],
            file.Entries);
    }

    [Theory]
    [InlineData("1.1 dwarf")]
    [InlineData("a.1: dwarf")]
    [InlineData("1: dwarf")]
    [InlineData("0.1: dwarf")]
    public void Parse_RejectsBadLines(string line)
    {
        Assert.Throws<FormatException>(() => SheetTagFile.Parse([line]));
    }

    [Fact]
    public void Parse_RejectsDuplicatePositions()
    {
        Assert.Throws<FormatException>(() => SheetTagFile.Parse(["1.1: elf", "01.1: dwarf"]));
    }

    [Fact]
    public void ImportKey_FollowsKindAndSheet()
    {
        Assert.Equal("portraits/dwarves_1/7.8", new SheetTagEntry(7, 8, "dwarf").ImportKey(ImageKind.Portrait, "dwarves_1"));
        Assert.Equal("icons/items_1/1.2", new SheetTagEntry(1, 2, "dagger").ImportKey(ImageKind.Icon, "items_1"));
    }

    [Fact]
    public void Template_ListsEveryPosition()
    {
        var lines = SheetTagFile.Template("elves_1", [(1, 1), (1, 2)]).ToList();
        var file = SheetTagFile.Parse(lines);

        Assert.Equal("elves_1", file.Source);
        Assert.Equal(["1.1", "1.2"], file.Entries.Select(e => e.Position));
        Assert.All(file.Entries, e => Assert.Equal("", e.Tags));
    }

    [Fact]
    public void Suggest_PutsExistingCategoriesFirst()
    {
        var tags = new[] { new TagCount("beard", 40), new TagCount("dwarf", 30), new TagCount("man", 20), new TagCount("elf", 10) };

        Assert.Equal(["elf", "dwarf", "beard", "man"], PortraitTags.Suggest(tags, 12));
        Assert.Equal(["elf", "dwarf", "beard"], PortraitTags.Suggest(tags, 3));
    }
}
