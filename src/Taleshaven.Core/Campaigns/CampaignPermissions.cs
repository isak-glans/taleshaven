using Taleshaven.Core.Threads;

namespace Taleshaven.Core.Campaigns;

/// <summary>
/// Behörighetsregler per kampanj, se avsnitt 4 i docs/projektbeskrivning.md.
/// Alla inloggade får läsa (arbetsförslag F1), därför finns ingen läsregel här än.
/// </summary>
public static class CampaignPermissions
{
    /// <summary>
    /// GM får alltid skriva. Spelare får skriva i öppna kanaler så länge kampanjen inte är stängd eller arkiverad.
    /// </summary>
    public static bool CanWritePost(CampaignRole role, CampaignStatus campaignStatus, ThreadStatus threadStatus) => role switch
    {
        CampaignRole.GameMaster => true,
        CampaignRole.Player => threadStatus == ThreadStatus.Open && IsActive(campaignStatus),
        _ => false,
    };

    private static bool IsActive(CampaignStatus status) =>
        status is CampaignStatus.OpenForApplications or CampaignStatus.Ongoing;
}
