using Microsoft.AspNetCore.Identity;

namespace Taleshaven.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}
