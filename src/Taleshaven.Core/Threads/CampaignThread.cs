namespace Taleshaven.Core.Threads;

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ThreadStatus
{
    Active = 0,

    /// <summary>Avslutad men fortfarande en del av kampanjens historia. Skrivskyddad för spelarna (B32).</summary>
    Completed = 1,
}

/// <summary>
/// En tråd i en kampanj (B25, B37). Trådar har bara en titel och en status; vill gruppen ha en krönika
/// skapar GM en egen tråd för den. GM skapar trådarna och styr deras ordning.
/// </summary>
public class CampaignThread
{
    private CampaignThread() { }

    public int Id { get; private set; }
    public int CampaignId { get; private set; }
    public string Title { get; private set; } = "";
    public ThreadStatus Status { get; private set; }

    /// <summary>Ordningen i trådlistan, som GM styr (B27). Lägst först.</summary>
    public int Position { get; private set; }

    public string CreatedById { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CampaignThread Create(int campaignId, string? title, int position, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        var thread = new CampaignThread
        {
            CampaignId = campaignId,
            Status = ThreadStatus.Active,
            Position = position,
            CreatedById = createdById,
            CreatedAt = now,
        };
        thread.Rename(title, now);
        return thread;
    }

    public void Rename(string? title, DateTimeOffset now)
    {
        var trimmed = title?.Trim() ?? "";
        if (trimmed.Length == 0)
            throw new CampaignRuleException("The thread needs a title.");
        if (trimmed.Length > ThreadLimits.TitleMaxLength)
            throw new CampaignRuleException($"The title can be at most {ThreadLimits.TitleMaxLength} characters.");

        Title = trimmed;
        UpdatedAt = now;
    }

    /// <summary>Avslutar tråden (B32). Spelarna kan inte längre skriva i den; GM kan.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status == ThreadStatus.Completed)
            throw new CampaignRuleException("The thread is already completed.");

        Status = ThreadStatus.Completed;
        UpdatedAt = now;
    }

    /// <summary>Öppnar en avslutad tråd igen, t.ex. om den avslutades av misstag (B32).</summary>
    public void Reopen(DateTimeOffset now)
    {
        if (Status == ThreadStatus.Active)
            throw new CampaignRuleException("The thread is already active.");

        Status = ThreadStatus.Active;
        UpdatedAt = now;
    }

    public void MoveTo(int position) => Position = position;
}
