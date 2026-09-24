namespace Taleshaven.Web;

/// <summary>
/// Meddelar öppna chattvyer (i alla anslutna kretsar) att ett inlägg har skapats eller redigerats.
/// Fungerar inom en serverinstans; vid flera instanser behövs t.ex. Redis eller Postgres LISTEN/NOTIFY.
/// </summary>
public sealed class PostNotifier
{
    public event Action<int>? PostCreated;

    /// <summary>Kanalens id och inläggets id.</summary>
    public event Action<int, long>? PostEdited;

    public void NotifyPostCreated(int threadId) => PostCreated?.Invoke(threadId);

    public void NotifyPostEdited(int threadId, long postId) => PostEdited?.Invoke(threadId, postId);
}
