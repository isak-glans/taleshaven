using Microsoft.AspNetCore.Identity;

namespace Taleshaven.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";

    /// <summary>Profilbild ur porträttbiblioteket (B50), eller null för initialer.</summary>
    public int? PortraitId { get; set; }

    /// <summary>Användarens egen beskrivning på profilsidan, i Markdown (B52).</summary>
    public string About { get; set; } = "";

    /// <summary>När kontot skapades (B52). Null för konton från tiden innan det sparades, om det inte gick att räkna fram.</summary>
    public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
