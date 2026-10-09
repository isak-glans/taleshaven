namespace Taleshaven.Core.Campaigns;

public interface ICampaignService
{
    /// <summary>
    /// Kampanjlistan (B48, B49), filtrerad på <paramref name="query"/>: varje sökord ska matcha början av ett ord i namnet
    /// eller en tagg. Ordnad efter status och sedan nyast först.
    /// </summary>
    Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(string viewerId, string? query = null, CancellationToken cancellationToken = default);

    /// <summary>Taggarna som används på kampanjer, de vanligaste först, som förslag när GM sätter taggar (B47).</summary>
    Task<IReadOnlyList<string>> GetCampaignTagsAsync(int limit = 20, CancellationToken cancellationToken = default);

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

public sealed record NewCampaign(string Name, string? Description, int? MaxPlayers, string? Tags = null, string? DefaultRoll = null);

public sealed record CampaignSettings(string? Name, string? Description, int? MaxPlayers, CampaignStatus Status,
    string? Tags = null, string? DefaultRoll = null);

public sealed record CampaignListItem(
    int Id,
    string Name,
    string GameMasterName,
    IReadOnlyList<string> Tags,
    int PlayerCount,
    int? MaxPlayers,
    CampaignStatus Status,
    CampaignRole ViewerRole,
    bool ViewerHasPendingApplication)
{
    /// <summary>Kampanjer man är GM eller spelare i visas under "My campaigns" (B48).</summary>
    public bool IsViewerIn => ViewerRole is CampaignRole.GameMaster or CampaignRole.Player;

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
    ViewerApplication? ViewerApplication,
    IReadOnlyList<string> Tags,
    string DefaultRoll)
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
