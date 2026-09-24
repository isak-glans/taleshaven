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

    /// <summary>Endast GM skriver krönikan (C-7). Gäller även stängda och arkiverade kampanjer.</summary>
    public static bool CanEditChronicle(CampaignRole role) => role == CampaignRole.GameMaster;

    /// <summary>Deltagare skapar karaktärer: spelare egna, GM NPC:er (H-1, H-6).</summary>
    public static bool CanCreateCharacter(CampaignRole role) => role is CampaignRole.Player or CampaignRole.GameMaster;

    /// <summary>Ägaren redigerar sin karaktär så länge hen deltar; GM redigerar alla i kampanjen (H-5).</summary>
    public static bool CanEditCharacter(CampaignRole role, string userId, string ownerId) => role switch
    {
        CampaignRole.GameMaster => true,
        CampaignRole.Player => userId == ownerId,
        _ => false,
    };

    /// <summary>Spelare skriver som sina egna karaktärer, GM som NPC:er (P-3).</summary>
    public static bool CanPostAsCharacter(CampaignRole role, string userId, string ownerId, bool isNpc) => role switch
    {
        CampaignRole.GameMaster => isNpc,
        CampaignRole.Player => !isNpc && userId == ownerId,
        _ => false,
    };

    private static bool IsActive(CampaignStatus status) =>
        status is CampaignStatus.OpenForApplications or CampaignStatus.Ongoing;
}
