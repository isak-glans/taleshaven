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

    /// <summary>Avstängd till den här tidpunkten (B70): kan läsa men inte skriva inlägg, söka till kampanjer eller skapa dem.</summary>
    public DateTimeOffset? SuspendedUntil { get; set; }

    /// <summary>När användaren godkände ordningsreglerna vid registreringen (B70). Null för äldre konton.</summary>
    public DateTimeOffset? AcceptedRulesAt { get; set; }
}
