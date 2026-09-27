namespace Taleshaven.Core.Threads;

/// <summary>
/// Hur långt en användare har läst i en tråd (F2, B28). Inlägg med högre id än <see cref="LastReadPostId"/>,
/// skrivna av någon annan och inte borttagna, räknas som olästa.
/// </summary>
public class ReadMarker
{
    private ReadMarker() { }

    public string UserId { get; private set; } = "";
    public int ThreadId { get; private set; }
    public long LastReadPostId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

/// <summary>
/// Olästmarkeringar. Bara kampanjens deltagare har läsposition; för andra är allt "läst".
/// </summary>
public interface IUnreadService
{
    /// <summary>Första olästa inlägget i tråden, eller null om allt är läst (eller användaren inte deltar).</summary>
    Task<long?> GetFirstUnreadPostIdAsync(int threadId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Flyttar läspositionen framåt (aldrig bakåt). Gör inget för den som inte deltar i kampanjen.</summary>
    Task MarkReadAsync(int threadId, string userId, long lastPostId, CancellationToken cancellationToken = default);

    /// <summary>Antal olästa inlägg per tråd i kampanjen. Trådar utan olästa saknas.</summary>
    Task<IReadOnlyDictionary<int, int>> GetThreadUnreadAsync(int campaignId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Antal olästa inlägg per kampanj där användaren deltar (GM eller spelare). Kampanjer utan olästa saknas.</summary>
    Task<IReadOnlyDictionary<int, int>> GetUnreadTotalsAsync(string userId, CancellationToken cancellationToken = default);
}

public static class UnreadDisplay
{
    public const int MaxShown = 99;

    public static string Format(int count) => count > MaxShown ? $"{MaxShown}+" : count.ToString();
}
