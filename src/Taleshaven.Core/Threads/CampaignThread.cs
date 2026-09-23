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
/// En tråd med inlägg. RPG-trådar skapas av GM; varje kampanj har dessutom exakt en OOC-tråd.
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

    public static CampaignThread CreateRpg(int campaignId, string? title, string? description, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        title = title?.Trim() ?? "";
        if (title.Length == 0)
            throw new CampaignRuleException("Tråden måste ha en titel.");
        if (title.Length > ThreadLimits.TitleMaxLength)
            throw new CampaignRuleException($"Titeln får vara högst {ThreadLimits.TitleMaxLength} tecken.");

        description = description?.Trim() ?? "";
        if (description.Length > ThreadLimits.DescriptionMaxLength)
            throw new CampaignRuleException($"Beskrivningen får vara högst {ThreadLimits.DescriptionMaxLength} tecken.");

        return new CampaignThread
        {
            CampaignId = campaignId,
            Kind = ThreadKind.Rpg,
            Title = title,
            Description = description,
            Status = ThreadStatus.Open,
            CreatedById = createdById,
            CreatedAt = now,
        };
    }

    public static CampaignThread CreateOoc(int campaignId, string createdById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdById);

        return new CampaignThread
        {
            CampaignId = campaignId,
            Kind = ThreadKind.Ooc,
            Title = "OOC",
            Status = ThreadStatus.Open,
            CreatedById = createdById,
            CreatedAt = now,
        };
    }

    public void SetLocked(bool locked)
    {
        if (Kind != ThreadKind.Rpg)
            throw new CampaignRuleException("Endast RPG-trådar kan låsas.");

        Status = locked ? ThreadStatus.Locked : ThreadStatus.Open;
    }
}
