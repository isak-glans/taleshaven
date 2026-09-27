namespace Taleshaven.Core.Threads;

// Värdena lagras i databasen. Ändra inte befintliga värden.
// Före fas 6 hette de Rpg och Ooc; befintliga RPG-kanaler är alltså Story-trådar och OOC-kanaler Discussion-trådar.
public enum ThreadKind
{
    /// <summary>En del av berättelsen, t.ex. ett kapitel eller en scen. Kan få en krönika när den avslutas (B33).</summary>
    Story = 0,

    /// <summary>Allt utanför berättelsen: OOC, regelfrågor, planering (B25).</summary>
    Discussion = 1,
}

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ThreadStatus
{
    Active = 0,

    /// <summary>Avslutad men fortfarande en del av kampanjens historia. Skrivskyddad för spelarna (B32).</summary>
    Completed = 1,
}

/// <summary>
/// En tråd i en kampanj (B25): en Story-tråd för berättelsen eller en Discussion-tråd för allt annat.
/// GM skapar trådarna och styr deras ordning. En Story-tråd får en krönika som visas i stället för introduktionen
/// när den är avslutad (B33).
/// </summary>
public class CampaignThread
{
    private CampaignThread() { }

    public int Id { get; private set; }
    public int CampaignId { get; private set; }
    public ThreadKind Kind { get; private set; }
    public string Title { get; private set; } = "";

    /// <summary>Introduktion i Markdown: scenens början för en Story-tråd, syftet för en Discussion-tråd.</summary>
    public string Introduction { get; private set; } = "";

    public ThreadStatus Status { get; private set; }

    /// <summary>Sammanfattning i Markdown av vad som hände i en Story-tråd (B33). Null tills den skrivs.</summary>
    public string? Chronicle { get; private set; }

    public string? ChronicleEditedById { get; private set; }
    public DateTimeOffset? ChronicleEditedAt { get; private set; }

    /// <summary>Ordningen i trådlistan, som GM styr (B27). Lägst först.</summary>
    public int Position { get; private set; }

    public string CreatedById { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CampaignThread Create(
        int campaignId, ThreadKind kind, string? title, string? introduction, int position, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);
        if (!Enum.IsDefined(kind))
            throw new CampaignRuleException("Unknown thread type.");

        var thread = new CampaignThread
        {
            CampaignId = campaignId,
            Kind = kind,
            Status = ThreadStatus.Active,
            Position = position,
            CreatedById = createdById,
            CreatedAt = now,
        };
        thread.UpdateDetails(title, introduction, now);
        return thread;
    }

    public void UpdateDetails(string? title, string? introduction, DateTimeOffset now)
    {
        var trimmedTitle = title?.Trim() ?? "";
        if (trimmedTitle.Length == 0)
            throw new CampaignRuleException("The thread needs a title.");
        if (trimmedTitle.Length > ThreadLimits.TitleMaxLength)
            throw new CampaignRuleException($"The title can be at most {ThreadLimits.TitleMaxLength} characters.");

        var trimmedIntroduction = introduction?.Trim() ?? "";
        if (trimmedIntroduction.Length > ThreadLimits.IntroductionMaxLength)
            throw new CampaignRuleException($"The introduction can be at most {ThreadLimits.IntroductionMaxLength} characters.");

        Title = trimmedTitle;
        Introduction = trimmedIntroduction;
        UpdatedAt = now;
    }

    /// <summary>
    /// Avslutar tråden (B32). För en Story-tråd kan krönikan skrivas i samma steg; utan text lämnas den orörd,
    /// så att den kan skrivas senare.
    /// </summary>
    public void Complete(string? chronicle, string editorId, DateTimeOffset now)
    {
        if (Status == ThreadStatus.Completed)
            throw new CampaignRuleException("The thread is already completed.");

        if (!string.IsNullOrWhiteSpace(chronicle))
            SetChronicle(chronicle, editorId, now);

        Status = ThreadStatus.Completed;
        UpdatedAt = now;
    }

    /// <summary>Öppnar en avslutad tråd igen, t.ex. om den avslutades av misstag (B32). Krönikan ligger kvar.</summary>
    public void Reopen(DateTimeOffset now)
    {
        if (Status == ThreadStatus.Active)
            throw new CampaignRuleException("The thread is already active.");

        Status = ThreadStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>Skriver eller ändrar krönikan (B33). Bara Story-trådar har krönika. Tom text tar bort den.</summary>
    public void SetChronicle(string? chronicle, string editorId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editorId);
        if (Kind != ThreadKind.Story)
            throw new CampaignRuleException("Only story threads have a chronicle.");

        var text = chronicle?.Trim() ?? "";
        if (text.Length > ThreadLimits.ChronicleMaxLength)
            throw new CampaignRuleException($"The chronicle can be at most {ThreadLimits.ChronicleMaxLength} characters.");

        Chronicle = text.Length == 0 ? null : text;
        ChronicleEditedById = editorId;
        ChronicleEditedAt = now;
        UpdatedAt = now;
    }

    public void MoveTo(int position) => Position = position;
}
