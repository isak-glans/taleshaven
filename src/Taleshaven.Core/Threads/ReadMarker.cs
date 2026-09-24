namespace Taleshaven.Core.Threads;

/// <summary>
/// Hur långt en användare har läst i en kanal (arbetsförslag F2). Inlägg med högre id än
/// <see cref="LastReadPostId"/>, skrivna av någon annan, räknas som olästa.
/// </summary>
public class ReadMarker
{
    private ReadMarker() { }

    public string UserId { get; private set; } = "";
    public int ThreadId { get; private set; }
    public long LastReadPostId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

/// <summary>Olästa inlägg per kanal i en kampanj, för flikarna i spelrummet.</summary>
public sealed record UnreadCounts(int RpgThreadId, int Rpg, int OocThreadId, int Ooc)
{
    public static readonly UnreadCounts None = new(0, 0, 0, 0);

    public bool Concerns(int threadId) => threadId == RpgThreadId || threadId == OocThreadId;
}

/// <summary>
/// Olästmarkeringar. Bara kampanjens deltagare har läsposition; för andra är allt "läst".
/// </summary>
public interface IUnreadService
{
    /// <summary>Senast lästa inlägg i kanalen, eller null om användaren aldrig har läst där.</summary>
    Task<long?> GetLastReadPostIdAsync(int threadId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Flyttar läspositionen framåt (aldrig bakåt). Gör inget för den som inte deltar i kampanjen.</summary>
    Task MarkReadAsync(int threadId, string userId, long lastPostId, CancellationToken cancellationToken = default);

    Task<UnreadCounts> GetUnreadCountsAsync(int campaignId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Antal olästa inlägg per kampanj där användaren deltar (GM eller spelare). Kampanjer utan olästa saknas.</summary>
    Task<IReadOnlyDictionary<int, int>> GetUnreadTotalsAsync(string userId, CancellationToken cancellationToken = default);
}

public static class UnreadDisplay
{
    public const int MaxShown = 99;

    public static string Format(int count) => count > MaxShown ? $"{MaxShown}+" : count.ToString();
}
