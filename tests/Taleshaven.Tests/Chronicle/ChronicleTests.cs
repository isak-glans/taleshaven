using Taleshaven.Core;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Chronicle;

namespace Taleshaven.Tests.Chronicle;

public class ChronicleChapterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsAndSetsPosition()
    {
        var chapter = ChronicleChapter.Create(7, 3, "  Den mörka skogen ", "  Spelarna stred mot fyra spindlar.  ", "gm", Now);

        Assert.Equal(7, chapter.CampaignId);
        Assert.Equal(3, chapter.Position);
        Assert.Equal("Den mörka skogen", chapter.Title);
        Assert.Equal("Spelarna stred mot fyra spindlar.", chapter.Content);
        Assert.Equal(Now, chapter.CreatedAt);
        Assert.Equal(Now, chapter.UpdatedAt);
    }

    [Theory]
    [InlineData(null, "Text")]
    [InlineData("  ", "Text")]
    [InlineData("Titel", null)]
    [InlineData("Titel", "   ")]
    public void Create_RequiresTitleAndContent(string? title, string? content)
    {
        Assert.Throws<CampaignRuleException>(() => ChronicleChapter.Create(7, 1, title, content, "gm", Now));
    }

    [Fact]
    public void Content_MaxFiveThousandCharacters()
    {
        ChronicleChapter.Create(7, 1, "Titel", new string('a', ChronicleLimits.ContentMaxLength), "gm", Now);

        Assert.Throws<CampaignRuleException>(() =>
            ChronicleChapter.Create(7, 1, "Titel", new string('a', ChronicleLimits.ContentMaxLength + 1), "gm", Now));
        Assert.Equal(5_000, ChronicleLimits.ContentMaxLength);
    }

    [Fact]
    public void Title_HasMaxLength()
    {
        Assert.Throws<CampaignRuleException>(() =>
            ChronicleChapter.Create(7, 1, new string('a', ChronicleLimits.TitleMaxLength + 1), "Text", "gm", Now));
    }

    [Fact]
    public void Update_ChangesTextAndUpdatedAtButNotCreatedAt()
    {
        var chapter = ChronicleChapter.Create(7, 1, "Titel", "Text", "gm", Now);
        var later = Now.AddDays(2);

        chapter.Update("Ny titel", "Ny text", later);

        Assert.Equal("Ny titel", chapter.Title);
        Assert.Equal("Ny text", chapter.Content);
        Assert.Equal(Now, chapter.CreatedAt);
        Assert.Equal(later, chapter.UpdatedAt);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(12, 12)]
    public void PageOf_OneChapterPerPage(int number, int expectedPage)
    {
        Assert.Equal(1, ChronicleLimits.ChaptersPerPage);
        Assert.Equal(expectedPage, ChronicleLimits.PageOf(number));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(12, 12)]
    public void PageCount_IsAtLeastOne(int chapters, int expectedPages)
    {
        Assert.Equal(expectedPages, ChronicleLimits.PageCount(chapters));
    }

    [Theory]
    [InlineData(1, 1, "1")]
    [InlineData(1, 5, "1 2 3 4 5")]
    [InlineData(3, 7, "1 2 3 4 5 6 7")]
    [InlineData(1, 20, "1 2 3 … 20")]
    [InlineData(10, 20, "1 … 8 9 10 11 12 … 20")]
    [InlineData(20, 20, "1 … 18 19 20")]
    [InlineData(5, 20, "1 2 3 4 5 6 7 … 20")]
    public void PageWindow_ShowsEdgesNeighboursAndGaps(int page, int pageCount, string expected)
    {
        var window = ChronicleLimits.PageWindow(page, pageCount);

        Assert.Equal(expected, string.Join(" ", window.Select(p => p?.ToString() ?? "…")));
    }

    [Theory]
    [InlineData(CampaignRole.GameMaster, true)]
    [InlineData(CampaignRole.Player, false)]
    [InlineData(CampaignRole.None, false)]
    public void OnlyGameMasterEditsChronicle(CampaignRole role, bool expected)
    {
        Assert.Equal(expected, CampaignPermissions.CanEditChronicle(role));
    }
}

public class ChronicleOrderingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    private static List<ChronicleChapter> Book(params string[] titles) =>
        titles.Select((title, i) => ChronicleChapter.Create(7, i + 1, title, "Text", "gm", Now)).ToList();

    private static string[] TitlesInOrder(IEnumerable<ChronicleChapter> chapters) =>
        chapters.OrderBy(c => c.Position).Select(c => c.Title).ToArray();

    [Fact]
    public void NextPosition_IsLastPlusOne()
    {
        Assert.Equal(1, ChronicleOrdering.NextPosition([]));
        Assert.Equal(4, ChronicleOrdering.NextPosition(Book("A", "B", "C")));
    }

    [Fact]
    public void Move_Earlier_SwapsWithPrevious()
    {
        var book = Book("A", "B", "C");

        Assert.True(ChronicleOrdering.Move(book, book[2], -1));

        Assert.Equal(["A", "C", "B"], TitlesInOrder(book));
        Assert.Equal(2, book[2].Position);
    }

    [Fact]
    public void Move_Later_SwapsWithNext()
    {
        var book = Book("A", "B", "C");

        Assert.True(ChronicleOrdering.Move(book, book[0], 1));

        Assert.Equal(["B", "A", "C"], TitlesInOrder(book));
    }

    [Fact]
    public void Move_PastEdges_DoesNothing()
    {
        var book = Book("A", "B", "C");

        Assert.False(ChronicleOrdering.Move(book, book[0], -1));
        Assert.False(ChronicleOrdering.Move(book, book[2], 1));
        Assert.Equal(["A", "B", "C"], TitlesInOrder(book));
    }

    [Fact]
    public void Move_RejectsInvalidDirection()
    {
        var book = Book("A", "B");

        Assert.Throws<ArgumentOutOfRangeException>(() => ChronicleOrdering.Move(book, book[0], 2));
    }

    [Fact]
    public void Move_RejectsChapterFromOtherBook()
    {
        var book = Book("A", "B");
        var stranger = Book("X")[0];

        Assert.Throws<CampaignRuleException>(() => ChronicleOrdering.Move(book, stranger, 1));
    }

    [Fact]
    public void Remove_RenumbersFollowingChapters()
    {
        var book = Book("A", "B", "C", "D");

        ChronicleOrdering.Remove(book, book[1]);

        var remaining = book.Where(c => c.Title != "B").ToList();
        Assert.Equal(["A", "C", "D"], TitlesInOrder(remaining));
        Assert.Equal([1, 2, 3], remaining.OrderBy(c => c.Position).Select(c => c.Position));
    }
}
