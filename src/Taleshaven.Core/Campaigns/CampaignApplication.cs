namespace Taleshaven.Core.Campaigns;

// Värdena lagras i databasen. Ändra inte befintliga värden.
public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public class CampaignApplication
{
    private CampaignApplication() { }

    internal CampaignApplication(string userId, string message, DateTimeOffset submittedAt)
    {
        Id = Guid.CreateVersion7(submittedAt);
        UserId = userId;
        Message = message;
        Status = ApplicationStatus.Pending;
        SubmittedAt = submittedAt;
    }

    public Guid Id { get; private set; }
    public int CampaignId { get; private set; }
    public string UserId { get; private set; } = "";
    public string Message { get; private set; } = "";
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? DecidedById { get; private set; }

    internal void Decide(ApplicationStatus decision, string decidedById, DateTimeOffset now)
    {
        Status = decision;
        DecidedById = decidedById;
        DecidedAt = now;
    }
}
