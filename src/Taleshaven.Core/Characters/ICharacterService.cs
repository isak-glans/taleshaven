namespace Taleshaven.Core.Characters;

/// <summary>
/// Kampanjens karaktärer. Alla inloggade får läsa; spelare skapar egna karaktärer och GM skapar NPC:er.
/// Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface ICharacterService
{
    /// <summary>Kampanjens karaktärer. Dolda NPC:er (B16) ingår bara för GM.</summary>
    Task<CharacterList> GetCharactersAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default);

    Task<CharacterDetails?> GetCharacterAsync(int campaignId, int characterId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Karaktärerna användaren kan skriva som i RPG-chatten (egna karaktärer, eller NPC:er för GM). Arkiverade NPC:er ingår inte.</summary>
    Task<IReadOnlyList<CharacterOption>> GetPostingOptionsAsync(int campaignId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Skapar en karaktär. GM:s karaktärer blir NPC:er. <paramref name="portraitId"/> väljs ur porträttbiblioteket (B19). Returnerar id.</summary>
    Task<int> CreateAsync(int campaignId, string userId, CharacterInput input, int? portraitId, CancellationToken cancellationToken = default);

    /// <summary>Uppdaterar karaktären och dess porträtt (null = initialer).</summary>
    Task UpdateAsync(int campaignId, int characterId, string userId, CharacterInput input, int? portraitId, CancellationToken cancellationToken = default);

    /// <summary>Arkiverar eller återställer en NPC (B17). Bara GM.</summary>
    Task SetArchivedAsync(int campaignId, int characterId, string userId, bool archived, CancellationToken cancellationToken = default);

    /// <summary>Tar bort karaktären. Går inte om den har skrivit inlägg, så att gamla inlägg behåller sin karaktär.</summary>
    Task DeleteAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default);
}

public sealed record CharacterList(
    IReadOnlyList<CharacterSummary> PlayerCharacters,
    IReadOnlyList<CharacterSummary> Npcs,
    IReadOnlyList<CharacterSummary> ArchivedNpcs,
    bool CanCreate);

public sealed record CharacterSummary(
    int Id,
    string Name,
    string OwnerId,
    string OwnerName,
    bool IsNpc,
    string? RuleSystem,
    string? AvatarUrl,
    bool IsArchived = false,
    bool IsHidden = false);

/// <summary>En karaktär att visa eller redigera. <see cref="GmNote"/> och <see cref="Alias"/> fylls bara i för kampanjens GM (B15, B16);
/// dolda NPC:er visas inte alls för andra.</summary>
public sealed record CharacterDetails(
    int Id,
    string Name,
    string OwnerName,
    bool IsNpc,
    string Sheet,
    string? SheetUrl,
    string? RuleSystem,
    string? AvatarUrl,
    int? PortraitId,
    DateTimeOffset UpdatedAt,
    bool CanEdit,
    string? GmNote = null,
    bool IsArchived = false,
    bool IsHidden = false,
    string? Alias = null);

/// <summary>Ett val i "Skriv som". <see cref="LastUsedAt"/> är när karaktären senast skrev ett inlägg (B17).</summary>
public sealed record CharacterOption(int Id, string Name, bool IsNpc, string? AvatarUrl, DateTimeOffset? LastUsedAt = null, bool IsHidden = false);
