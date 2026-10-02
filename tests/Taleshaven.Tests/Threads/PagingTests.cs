using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;

namespace Taleshaven.Tests.Threads;

public class PagingTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(250, 13)]
    public void PageCount_Uses20PostsPerPage(int posts, int expected)
    {
        Assert.Equal(expected, Paging.PageCount(posts));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    [InlineData(41, 3)]
    public void PageOf_FindsThePageOfAPost(int position, int expected)
    {
        Assert.Equal(expected, Paging.PageOf(position));
    }

    [Theory]
    [InlineData(1, 14, new[] { 1, 2, 3, 0, 14 })]
    [InlineData(13, 14, new[] { 1, 0, 11, 12, 13, 14 })]
    [InlineData(7, 14, new[] { 1, 0, 5, 6, 7, 8, 9, 0, 14 })]
    [InlineData(4, 14, new[] { 1, 2, 3, 4, 5, 6, 0, 14 })]
    [InlineData(2, 3, new[] { 1, 2, 3 })]
    public void Window_ShowsEllipsisForLongThreads(int page, int pageCount, int[] expected)
    {
        Assert.Equal(expected, Paging.Window(page, pageCount).Select(p => p ?? 0));
    }

    [Fact]
    public void Clamp_KeepsPageWithinThread()
    {
        Assert.Equal(1, Paging.Clamp(0, 5));
        Assert.Equal(5, Paging.Clamp(int.MaxValue, 5));
        Assert.Equal(1, Paging.Clamp(3, 0));
    }

    [Fact]
    public void Excerpt_StripsMarkdownAndCutsAtWord()
    {
        var excerpt = TextExcerpt.From("## The ruins\n\nThe party has reached the **old ruins** at dusk [dice:1].", 30);

        Assert.Equal("The ruins The party has…", excerpt);
        Assert.Equal("", TextExcerpt.From(null, 30));
        Assert.Equal("Short text", TextExcerpt.From("> Short *text*", 30));
    }
}
