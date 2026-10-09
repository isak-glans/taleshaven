using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Taleshaven.Core.Users;

namespace Taleshaven.Infrastructure.Identity;

public static class TaleshavenClaimTypes
{
    public const string DisplayName = "taleshaven:display_name";

    /// <summary>Adressen till profilbilden (B50), om användaren har en.</summary>
    public const string AvatarUrl = "taleshaven:avatar_url";
}

// Lägger visningsnamnet och profilbilden i inloggningskakan så att UI:t kan visa dem utan databasanrop.
public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor,
    IAccountService accounts)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(TaleshavenClaimTypes.DisplayName, user.DisplayName));
        if (user.PortraitId is not null && await accounts.GetProfilePortraitAsync(user.Id) is { } portrait)
            identity.AddClaim(new Claim(TaleshavenClaimTypes.AvatarUrl, portrait.Url));
        return identity;
    }
}
