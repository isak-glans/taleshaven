namespace Taleshaven.Core.Threads;

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ThreadStatus
{
    Active = 0,

    /// <summary>Avslutad men fortfarande en del av kampanjens historia. Skrivskyddad för spelarna (B32).</summary>
    Completed = 1,
}

/// <summary>
/// En tråd i en kampanj (B25, B37) eller i forumet (B72). Trådar har bara en titel och en status; vill gruppen ha en
/// krönika skapar GM en egen tråd för den. GM skapar kampanjens trådar och styr deras ordning. En forumtråd har ingen
/// kampanj (<see cref="CampaignId"/> är null) utan en kategori, och kan vara fäst och låst; den skapas av sajtens
/// administratörer och managers.
/// </summary>
public class CampaignThread
{
    private CampaignThread() { }

    public int Id { get; private set; }
    public int? CampaignId { get; private set; }

    /// <summary>Forumkategorin (B72), eller null för en kampanjtråd.</summary>
    public int? CategoryId { get; private set; }

    /// <summary>En fäst forumtråd ligger alltid överst (B72).</summary>
    public bool IsPinned { get; private set; }

    /// <summary>En låst forumtråd kan läsas men bara administratörer och managers kan skriva i den (B72).</summary>
    public bool IsLocked { get; private set; }

    public bool IsForum => CategoryId is not null;
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

    /// <summary>En tråd i forumets kategori (B72).</summary>
    public static CampaignThread CreateForumThread(int categoryId, string? title, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        var thread = new CampaignThread
        {
            CategoryId = categoryId,
            Status = ThreadStatus.Active,
            CreatedById = createdById,
            CreatedAt = now,
        };
        thread.Rename(title, now);
        return thread;
    }

    public void SetPinned(bool pinned, DateTimeOffset now)
    {
        IsPinned = pinned;
        UpdatedAt = now;
    }

    public void SetLocked(bool locked, DateTimeOffset now)
    {
        IsLocked = locked;
        UpdatedAt = now;
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
