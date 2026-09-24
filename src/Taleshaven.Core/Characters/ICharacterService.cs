namespace Taleshaven.Core.Characters;

/// <summary>
/// Kampanjens karaktärer. Alla inloggade får läsa; spelare skapar egna karaktärer och GM skapar NPC:er.
/// Regelbrott och saknad behörighet ger <see cref="CampaignRuleException"/>.
/// </summary>
public interface ICharacterService
{
    Task<CharacterList> GetCharactersAsync(int campaignId, string viewerId, CancellationToken cancellationToken = default);

    Task<CharacterDetails?> GetCharacterAsync(int campaignId, int characterId, string viewerId, CancellationToken cancellationToken = default);

    /// <summary>Karaktärerna användaren kan skriva som i RPG-chatten (egna karaktärer, eller NPC:er för GM).</summary>
    Task<IReadOnlyList<CharacterOption>> GetPostingOptionsAsync(int campaignId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Skapar en karaktär. GM:s karaktärer blir NPC:er. <paramref name="avatar"/> är en redan behandlad bild. Returnerar id.</summary>
    Task<int> CreateAsync(int campaignId, string userId, CharacterInput input, byte[]? avatar, CancellationToken cancellationToken = default);

    /// <summary>Uppdaterar karaktären. <paramref name="avatar"/> ersätter bilden; <paramref name="removeAvatar"/> tar bort den.</summary>
    Task UpdateAsync(int campaignId, int characterId, string userId, CharacterInput input, byte[]? avatar, bool removeAvatar, CancellationToken cancellationToken = default);

    /// <summary>Tar bort karaktären. Går inte om den har skrivit inlägg, så att gamla inlägg behåller sin karaktär.</summary>
    Task DeleteAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default);
}

public sealed record CharacterList(
    IReadOnlyList<CharacterSummary> PlayerCharacters,
    IReadOnlyList<CharacterSummary> Npcs,
    bool CanCreate);

public sealed record CharacterSummary(
    int Id,
    string Name,
    string OwnerId,
    string OwnerName,
    bool IsNpc,
    string? RuleSystem,
    string? AvatarUrl);

public sealed record CharacterDetails(
    int Id,
    string Name,
    string OwnerName,
    bool IsNpc,
    string Sheet,
    string? SheetUrl,
    string? RuleSystem,
    string? AvatarUrl,
    DateTimeOffset UpdatedAt,
    bool CanEdit);

public sealed record CharacterOption(int Id, string Name, bool IsNpc, string? AvatarUrl);
