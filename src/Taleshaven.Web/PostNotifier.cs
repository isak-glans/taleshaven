namespace Taleshaven.Web;

/// <summary>
/// Meddelar öppna trådvyer (i alla anslutna kretsar) att ett nytt inlägg har skapats.
/// Fungerar inom en serverinstans; vid flera instanser behövs t.ex. Redis eller Postgres LISTEN/NOTIFY.
/// </summary>
public sealed class PostNotifier
{
    public event Action<int>? PostCreated;

    public void NotifyPostCreated(int threadId) => PostCreated?.Invoke(threadId);
}
