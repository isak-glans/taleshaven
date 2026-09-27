namespace Taleshaven.Core.Threads;

public static class ThreadLimits
{
    public const int TitleMaxLength = 100;
    public const int IntroductionMaxLength = 5_000;
    public const int ChronicleMaxLength = 5_000;

    /// <summary>Längsta inlägg som kan skrivas eller redigeras (B23).</summary>
    public const int PostMaxLength = 5_000;

    /// <summary>Kolumnens storlek i databasen. Större än <see cref="PostMaxLength"/>, så att inlägg från tiden före B23 ligger kvar.</summary>
    public const int PostStorageMaxLength = 10_000;

    /// <summary>Inlägg per sida i en tråd (B28).</summary>
    public const int PostsPerPage = 25;

    /// <summary>Högst så många tärningsslag i ett inlägg (B31).</summary>
    public const int MaxRollsPerPost = 10;
}

/// <summary>Sidindelning av inläggen i en tråd (B28) och sidnavigeringen längst ner.</summary>
public static class Paging
{
    public static int PageCount(int itemCount, int perPage = ThreadLimits.PostsPerPage) =>
        Math.Max(1, (itemCount + perPage - 1) / perPage);

    /// <summary>Sidan där det n:te elementet (1-baserat) hamnar.</summary>
    public static int PageOf(int position, int perPage = ThreadLimits.PostsPerPage) =>
        Math.Max(1, (position + perPage - 1) / perPage);

    public static int Clamp(int page, int pageCount) => Math.Clamp(page, 1, Math.Max(1, pageCount));

    /// <summary>
    /// Sidnumren som visas i navigeringen: första, sista och några runt den aktuella. <c>null</c> betyder "…".
    /// En lucka på en enda sida visas som sidan själv i stället för "…". Exempel: 1 2 3 … 12 13 14.
    /// </summary>
    public static IReadOnlyList<int?> Window(int page, int pageCount, int neighbours = 2)
    {
        var shown = new SortedSet<int> { 1, pageCount };
        for (var p = page - neighbours; p <= page + neighbours; p++)
        {
            if (p >= 1 && p <= pageCount)
                shown.Add(p);
        }

        var result = new List<int?>();
        int? previous = null;
        foreach (var p in shown)
        {
            if (previous is { } prev && p - prev == 2)
                result.Add(prev + 1);
            else if (previous is not null && p - previous > 2)
                result.Add(null);
            result.Add(p);
            previous = p;
        }
        return result;
    }
}
