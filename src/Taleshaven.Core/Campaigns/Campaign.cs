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

    /// <summary>GM ändrar namn, beskrivning och max antal spelare (A-2).</summary>
    public void UpdateDetails(string? name, string? description, int? maxPlayers)
    {
        var validatedMax = ValidateMaxPlayers(maxPlayers);
        if (validatedMax < Memberships.Count)
            throw new CampaignRuleException($"The campaign already has {Memberships.Count} players. Remove players first or choose a higher number.");

        Name = ValidateName(name);
        Description = ValidateDescription(description);
        MaxPlayers = validatedMax;
    }

    /// <summary>GM öppnar, stänger, arkiverar eller återställer kampanjen (A-3).</summary>
    public void ChangeStatus(CampaignStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new CampaignRuleException("Unknown status.");

        Status = status;
    }

    /// <summary>GM tar bort en spelare ur kampanjen (A-4). Spelarens karaktärer och inlägg finns kvar.</summary>
    public void RemovePlayer(string userId)
    {
        var membership = Memberships.SingleOrDefault(m => m.UserId == userId)
            ?? throw new CampaignRuleException("The player is not in the campaign.");

        Memberships.Remove(membership);
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
            throw new CampaignRuleException("You are the GM of this campaign.");
        if (IsPlayer(userId))
            throw new CampaignRuleException("You are already in the campaign.");
        if (Applications.Any(a => a.UserId == userId && a.Status == ApplicationStatus.Pending))
            throw new CampaignRuleException("You already have an application waiting for an answer.");
        if (!AcceptsApplications)
            throw new CampaignRuleException("The campaign is not accepting applications right now.");

        message = message?.Trim() ?? "";
        if (message.Length > CampaignLimits.ApplicationMessageMaxLength)
            throw new CampaignRuleException($"The message can be at most {CampaignLimits.ApplicationMessageMaxLength} characters.");

        var application = new CampaignApplication(userId, message, now);
        Applications.Add(application);
        return application;
    }

    /// <summary>Den som ansökt drar tillbaka sin väntande ansökan (B22). Hen kan ansöka igen senare.</summary>
    public void WithdrawApplication(string userId, DateTimeOffset now)
    {
        var application = Applications.SingleOrDefault(a => a.UserId == userId && a.Status == ApplicationStatus.Pending)
            ?? throw new CampaignRuleException("You have no application waiting for an answer.");

        application.Decide(ApplicationStatus.Withdrawn, userId, now);
    }

    public void ApproveApplication(Guid applicationId, string gameMasterId, DateTimeOffset now)
    {
        var application = GetPendingApplicationForGameMaster(applicationId, gameMasterId);

        if (IsFull)
            throw new CampaignRuleException("The campaign is full.");

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
            throw new CampaignRuleException("Only the campaign's GM can handle applications.");

        return Applications.SingleOrDefault(a => a.Id == applicationId && a.Status == ApplicationStatus.Pending)
            ?? throw new CampaignRuleException("The application doesn't exist or has already been handled.");
    }

    // Valideringsfelen är CampaignRuleException så att de kan visas för GM som redigerar kampanjen.
    private static string ValidateName(string? name)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0)
            throw new CampaignRuleException("The campaign needs a name.");
        if (name.Length > CampaignLimits.NameMaxLength)
            throw new CampaignRuleException($"The name can be at most {CampaignLimits.NameMaxLength} characters.");
        return name;
    }

    private static string ValidateDescription(string? description)
    {
        description = description?.Trim() ?? "";
        if (description.Length > CampaignLimits.DescriptionMaxLength)
            throw new CampaignRuleException($"The description can be at most {CampaignLimits.DescriptionMaxLength} characters.");
        return description;
    }

    private static int? ValidateMaxPlayers(int? maxPlayers)
    {
        if (maxPlayers is < CampaignLimits.MinPlayers or > CampaignLimits.MaxPlayers)
            throw new CampaignRuleException($"Max players must be between {CampaignLimits.MinPlayers} and {CampaignLimits.MaxPlayers}.");
        return maxPlayers;
    }
}
