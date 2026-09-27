namespace Taleshaven.Web;

/// <summary>
/// Meddelar öppna trådsidor (i alla anslutna kretsar) att ett inlägg har skapats, redigerats eller tagits bort.
/// Fungerar inom en serverinstans; vid flera instanser behövs t.ex. Redis eller Postgres LISTEN/NOTIFY.
/// </summary>
public sealed class PostNotifier
{
    /// <summary>Kampanjens id och trådens id.</summary>
    public event Action<int, int>? PostCreated;

    /// <summary>Trådens id och inläggets id.</summary>
    public event Action<int, long>? PostChanged;

    public void NotifyPostCreated(int campaignId, int threadId) => PostCreated?.Invoke(campaignId, threadId);

    public void NotifyPostChanged(int threadId, long postId) => PostChanged?.Invoke(threadId, postId);
}
