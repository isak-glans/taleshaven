namespace Taleshaven.Core.Users;

/// <summary>Användarens eget konto. Regelbrott ger <see cref="CampaignRuleException"/>.</summary>
public interface IAccountService
{
    /// <summary>
    /// Tar bort kontot (B21). Inlägg, karaktärer och krönikekapitel ligger kvar men visas som <see cref="DeletedAccount.DisplayName"/>;
    /// e-post, lösenord, inloggningar, roller, medlemskap, ansökningar och läspositioner raderas. Går inte för den som är GM.
    /// </summary>
    Task DeleteAccountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Användarens profilbild (B50), eller null om hen visas med initialer.</summary>
    Task<ProfilePortrait?> GetProfilePortraitAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Profilsidan för en användare (B52), eller null om användaren inte finns eller kontot är borttaget.</summary>
    Task<UserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Väljer en profilbild ur porträttbiblioteket, eller tar bort den med <c>null</c> (B50).</summary>
    Task SetProfilePortraitAsync(string userId, int? portraitId, CancellationToken cancellationToken = default);
}

public sealed record ProfilePortrait(int PortraitId, string Url);

/// <summary>
/// En användares profilsida (B52): det andra inloggade ser. <see cref="PostCount"/> räknar inlägg som inte är borttagna.
/// </summary>
public sealed record UserProfile(
    string Id,
    string DisplayName,
    string? AvatarUrl,
    string About,
    DateTimeOffset? CreatedAt,
    int PostCount,
    IReadOnlyList<ProfileCampaign> Campaigns);

/// <summary>En kampanj som användaren är GM eller spelare i.</summary>
public sealed record ProfileCampaign(int Id, string Name, bool IsGameMaster, Campaigns.CampaignStatus Status);

public static class DeletedAccount
{
    /// <summary>Namnet som visas i stället för en borttagen användare.</summary>
    public const string DisplayName = "Deleted user";

    /// <summary>Användarnamnet på gravstenen efter ett borttaget konto; det kan inte användas för att logga in.</summary>
    public static string TombstoneUserName(string userId) => $"deleted-{userId}";

    public static bool IsTombstone(string userId, string? userName) => userName == TombstoneUserName(userId);

    /// <summary>Kastar om kontot inte får tas bort: en kampanj kan inte stå utan GM.</summary>
    public static void EnsureCanDelete(int campaignsAsGameMaster)
    {
        if (campaignsAsGameMaster > 0)
            throw new CampaignRuleException(campaignsAsGameMaster == 1
                ? "You are the GM of a campaign. Delete it (under Settings) before deleting your account."
                : $"You are the GM of {campaignsAsGameMaster} campaigns. Delete them (under Settings) before deleting your account.");
    }
}
