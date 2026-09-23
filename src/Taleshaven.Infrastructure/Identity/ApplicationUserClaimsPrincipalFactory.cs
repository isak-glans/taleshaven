using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Taleshaven.Infrastructure.Identity;

public static class TaleshavenClaimTypes
{
    public const string DisplayName = "taleshaven:display_name";
}

// Lägger visningsnamnet i inloggningskakan så att UI:t kan visa det utan databasanrop.
public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(TaleshavenClaimTypes.DisplayName, user.DisplayName));
        return identity;
    }
}
