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
