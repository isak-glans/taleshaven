using System.Security.Claims;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Web;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(TaleshavenClaimTypes.DisplayName) is { Length: > 0 } displayName
            ? displayName
            : user.Identity?.Name ?? "";
}
