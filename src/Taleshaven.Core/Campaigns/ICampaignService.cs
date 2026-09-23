namespace Taleshaven.Core.Campaigns;

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignListItem>> GetCampaignsAsync(CancellationToken cancellationToken = default);

    /// <summary>Skapar en kampanj där <paramref name="gameMasterId"/> blir GM. Returnerar kampanjens id.</summary>
    Task<int> CreateCampaignAsync(string gameMasterId, NewCampaign campaign, CancellationToken cancellationToken = default);
}

public sealed record NewCampaign(string Name, string? Description, int? MaxPlayers);

public sealed record CampaignListItem(
    int Id,
    string Name,
    string GameMasterName,
    string Description,
    int PlayerCount,
    int? MaxPlayers,
    CampaignStatus Status)
{
    public bool AcceptsApplications => Campaign.CanAcceptApplications(Status, MaxPlayers, PlayerCount);
}
