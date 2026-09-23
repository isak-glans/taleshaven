namespace Taleshaven.Core.Threads;

public static class ThreadLimits
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int PostMaxLength = 10_000;
}

/// <summary>
/// Hur många inlägg chatten visar när den öppnas och hur många som hämtas när man scrollar uppåt (beslut B8).
/// </summary>
public static class ChatWindow
{
    public const int InitialDays = 7;
    public const int InitialMinPosts = 20;
    public const int InitialMaxPosts = 100;
    public const int OlderPageSize = 20;

    /// <summary>
    /// Antal inlägg att visa vid öppning: de från de senaste <see cref="InitialDays"/> dagarna,
    /// men minst <see cref="InitialMinPosts"/> och högst <see cref="InitialMaxPosts"/> (och aldrig fler än som finns).
    /// </summary>
    /// <param name="newestFirst">Tidpunkter för de senaste inläggen, nyast först.</param>
    public static int InitialCount(IReadOnlyList<DateTimeOffset> newestFirst, DateTimeOffset now)
    {
        var cutoff = now.AddDays(-InitialDays);
        var recent = newestFirst.TakeWhile(createdAt => createdAt >= cutoff).Count();

        return Math.Min(Math.Max(recent, InitialMinPosts), Math.Min(InitialMaxPosts, newestFirst.Count));
    }
}
