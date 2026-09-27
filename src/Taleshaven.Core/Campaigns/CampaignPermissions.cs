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
        CampaignRole.Player => threadStatus == ThreadStatus.Active && IsActive(campaignStatus),
        _ => false,
    };

    /// <summary>
    /// Man redigerar sina egna inlägg så länge man får skriva i tråden (B12); GM redigerar alla inlägg i sin kampanj (B30).
    /// En spelare som lämnat kampanjen, eller en arkiverad kampanj, stänger alltså även redigering.
    /// </summary>
    public static bool CanEditPost(CampaignRole role, CampaignStatus campaignStatus, ThreadStatus threadStatus, string userId, string authorId) =>
        (userId == authorId || role == CampaignRole.GameMaster) && CanWritePost(role, campaignStatus, threadStatus);

    /// <summary>
    /// Samma regel som för redigering (B30), men inlägg med tärningsslag kan bara GM ta bort (B31), så att ingen
    /// kan ta bort ett dåligt slag.
    /// </summary>
    public static bool CanDeletePost(
        CampaignRole role, CampaignStatus campaignStatus, ThreadStatus threadStatus, string userId, string authorId, bool hasRolls) =>
        role == CampaignRole.GameMaster || (!hasRolls && CanEditPost(role, campaignStatus, threadStatus, userId, authorId));

    /// <summary>Endast GM skapar, ändrar, ordnar och avslutar trådar i första versionen (B25, B32).</summary>
    public static bool CanManageThreads(CampaignRole role) => role == CampaignRole.GameMaster;

    /// <summary>Endast GM administrerar kampanjen: inställningar, status, spelare och radering (A-2–A-4).</summary>
    public static bool CanManageCampaign(CampaignRole role) => role == CampaignRole.GameMaster;

    /// <summary>Endast GM skriver krönikan (C-7, B33). Gäller även stängda och arkiverade kampanjer.</summary>
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
