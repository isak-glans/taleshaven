namespace Taleshaven.Core.Campaigns;

public class Campaign
{
    private Campaign() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string GameMasterId { get; private set; } = "";
    public int? MaxPlayers { get; private set; }
    public CampaignStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<CampaignMembership> Memberships { get; private set; } = [];

    // Vid laddning från databasen räcker det att ta med ansökningar som väntar på beslut.
    public List<CampaignApplication> Applications { get; private set; } = [];

    public static Campaign Create(string gameMasterId, string name, string? description, int? maxPlayers, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameMasterId);

        return new Campaign
        {
            GameMasterId = gameMasterId,
            Name = ValidateName(name),
            Description = ValidateDescription(description),
            MaxPlayers = ValidateMaxPlayers(maxPlayers),
            Status = CampaignStatus.OpenForApplications,
            CreatedAt = now,
        };
    }

    public bool IsGameMaster(string? userId) => userId is not null && userId == GameMasterId;

    public bool IsPlayer(string? userId) => userId is not null && Memberships.Any(m => m.UserId == userId);

    public bool AcceptsApplications => CanAcceptApplications(Status, MaxPlayers, Memberships.Count);

    public static bool CanAcceptApplications(CampaignStatus status, int? maxPlayers, int playerCount) =>
        status == CampaignStatus.OpenForApplications && (maxPlayers is null || playerCount < maxPlayers);

    public bool IsFull => MaxPlayers is not null && Memberships.Count >= MaxPlayers;

    public CampaignApplication Apply(string userId, string? message, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (IsGameMaster(userId))
            throw new CampaignRuleException("Du är GM i den här kampanjen.");
        if (IsPlayer(userId))
            throw new CampaignRuleException("Du deltar redan i kampanjen.");
        if (Applications.Any(a => a.UserId == userId && a.Status == ApplicationStatus.Pending))
            throw new CampaignRuleException("Du har redan en ansökan som väntar på svar.");
        if (!AcceptsApplications)
            throw new CampaignRuleException("Kampanjen tar inte emot ansökningar just nu.");

        message = message?.Trim() ?? "";
        if (message.Length > CampaignLimits.ApplicationMessageMaxLength)
            throw new CampaignRuleException($"Meddelandet får vara högst {CampaignLimits.ApplicationMessageMaxLength} tecken.");

        var application = new CampaignApplication(userId, message, now);
        Applications.Add(application);
        return application;
    }

    public void ApproveApplication(Guid applicationId, string gameMasterId, DateTimeOffset now)
    {
        var application = GetPendingApplicationForGameMaster(applicationId, gameMasterId);

        if (IsFull)
            throw new CampaignRuleException("Kampanjen är fullsatt.");

        application.Decide(ApplicationStatus.Approved, gameMasterId, now);
        Memberships.Add(new CampaignMembership(application.UserId, now));
    }

    public void RejectApplication(Guid applicationId, string gameMasterId, DateTimeOffset now)
    {
        var application = GetPendingApplicationForGameMaster(applicationId, gameMasterId);
        application.Decide(ApplicationStatus.Rejected, gameMasterId, now);
    }

    private CampaignApplication GetPendingApplicationForGameMaster(Guid applicationId, string gameMasterId)
    {
        if (!IsGameMaster(gameMasterId))
            throw new CampaignRuleException("Endast kampanjens GM kan hantera ansökningar.");

        return Applications.SingleOrDefault(a => a.Id == applicationId && a.Status == ApplicationStatus.Pending)
            ?? throw new CampaignRuleException("Ansökan finns inte eller är redan behandlad.");
    }

    private static string ValidateName(string? name)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0)
            throw new ArgumentException("Kampanjen måste ha ett namn.", nameof(name));
        if (name.Length > CampaignLimits.NameMaxLength)
            throw new ArgumentException($"Namnet får vara högst {CampaignLimits.NameMaxLength} tecken.", nameof(name));
        return name;
    }

    private static string ValidateDescription(string? description)
    {
        description = description?.Trim() ?? "";
        if (description.Length > CampaignLimits.DescriptionMaxLength)
            throw new ArgumentException($"Beskrivningen får vara högst {CampaignLimits.DescriptionMaxLength} tecken.", nameof(description));
        return description;
    }

    private static int? ValidateMaxPlayers(int? maxPlayers)
    {
        if (maxPlayers is < CampaignLimits.MinPlayers or > CampaignLimits.MaxPlayers)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), maxPlayers,
                $"Max antal spelare måste vara mellan {CampaignLimits.MinPlayers} och {CampaignLimits.MaxPlayers}.");
        return maxPlayers;
    }
}
