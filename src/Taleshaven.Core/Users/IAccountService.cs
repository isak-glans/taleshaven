namespace Taleshaven.Core.Users;

/// <summary>Användarens eget konto. Regelbrott ger <see cref="CampaignRuleException"/>.</summary>
public interface IAccountService
{
    /// <summary>
    /// Tar bort kontot (B21). Inlägg, karaktärer och krönikekapitel ligger kvar men visas som <see cref="DeletedAccount.DisplayName"/>;
    /// e-post, lösenord, inloggningar, roller, medlemskap, ansökningar och läspositioner raderas. Går inte för den som är GM.
    /// </summary>
    Task DeleteAccountAsync(string userId, CancellationToken cancellationToken = default);
}

public static class DeletedAccount
{
    /// <summary>Namnet som visas i stället för en borttagen användare.</summary>
    public const string DisplayName = "Deleted user";

    /// <summary>Kastar om kontot inte får tas bort: en kampanj kan inte stå utan GM.</summary>
    public static void EnsureCanDelete(int campaignsAsGameMaster)
    {
        if (campaignsAsGameMaster > 0)
            throw new CampaignRuleException(campaignsAsGameMaster == 1
                ? "You are the GM of a campaign. Delete it (under Settings) before deleting your account."
                : $"You are the GM of {campaignsAsGameMaster} campaigns. Delete them (under Settings) before deleting your account.");
    }
}
