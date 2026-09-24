namespace Taleshaven.Core.Campaigns;

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(string viewerId, CancellationToken cancellationToken = default);

    Task<CampaignDetails?> GetCampaignAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Skapar en kampanj där <paramref name="gameMasterId"/> blir GM. Returnerar kampanjens id.</summary>
    Task<int> CreateCampaignAsync(string gameMasterId, NewCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>GM ändrar kampanjens inställningar och status (A-2, A-3). Regelbrott ger <see cref="CampaignRuleException"/>.</summary>
    Task UpdateCampaignAsync(int campaignId, string userId, CampaignSettings settings, CancellationToken cancellationToken = default);

    /// <summary>GM tar bort en spelare ur kampanjen (A-4).</summary>
    Task RemovePlayerAsync(int campaignId, string userId, string playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raderar kampanjen och allt innehåll för gott. <paramref name="confirmationName"/> måste vara kampanjens namn,
    /// som skydd mot att radera av misstag (A-3).
    /// </summary>
    Task DeleteCampaignAsync(int campaignId, string userId, string? confirmationName, CancellationToken cancellationToken = default);
}

public sealed record NewCampaign(string Name, string? Description, int? MaxPlayers);

public sealed record CampaignSettings(string? Name, string? Description, int? MaxPlayers, CampaignStatus Status);

public sealed record CampaignListItem(
    int Id,
    string Name,
    string GameMasterName,
    string Description,
    int PlayerCount,
    int? MaxPlayers,
    CampaignStatus Status,
    CampaignRole ViewerRole,
    bool ViewerHasPendingApplication)
{
    public bool AcceptsApplications => Campaign.CanAcceptApplications(Status, MaxPlayers, PlayerCount);

    public bool CanViewerApply => ViewerRole == CampaignRole.None && !ViewerHasPendingApplication && AcceptsApplications;
}

public sealed record CampaignDetails(
    int Id,
    string Name,
    string Description,
    string GameMasterName,
    int? MaxPlayers,
    CampaignStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CampaignPlayer> Players,
    CampaignRole ViewerRole,
    ViewerApplication? ViewerApplication)
{
    public bool AcceptsApplications => Campaign.CanAcceptApplications(Status, MaxPlayers, Players.Count);

    public bool CanViewerApply =>
        ViewerRole == CampaignRole.None
        && ViewerApplication?.Status != ApplicationStatus.Pending
        && AcceptsApplications;
}

public sealed record CampaignPlayer(string UserId, string DisplayName, DateTimeOffset JoinedAt);

/// <summary>Den inloggade användarens senaste ansökan till kampanjen.</summary>
public sealed record ViewerApplication(ApplicationStatus Status, DateTimeOffset SubmittedAt, DateTimeOffset? DecidedAt);
