namespace Taleshaven.Core.Threads;

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ThreadKind
{
    Rpg = 0,
    Ooc = 1,
}

public enum ThreadStatus
{
    Open = 0,
    Locked = 1,
}

/// <summary>
/// En chattkanal i en kampanj. Varje kampanj har en RPG-kanal och en OOC-kanal (beslut B6, B7).
/// Modellen tillåter fler kanaler per kampanj om det behövs senare.
/// </summary>
public class CampaignThread
{
    private CampaignThread() { }

    public int Id { get; private set; }
    public int CampaignId { get; private set; }
    public ThreadKind Kind { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public ThreadStatus Status { get; private set; }
    public string CreatedById { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static CampaignThread CreateChannel(int campaignId, ThreadKind kind, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        return new CampaignThread
        {
            CampaignId = campaignId,
            Kind = kind,
            Title = kind == ThreadKind.Rpg ? "RPG" : "OOC",
            Status = ThreadStatus.Open,
            CreatedById = createdById,
            CreatedAt = now,
        };
    }
}
