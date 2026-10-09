using Taleshaven.Core.Dice;

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

    /// <summary>
    /// Arkiverar eller återställer en karaktär (B17, B68). GM för NPC:er, ägaren eller GM för spelarkaraktärer. Att
    /// återställa en spelarkaraktär räknas mot taket på aktiva karaktärer (<see cref="CharacterLimits.MaxActivePlayerCharacters"/>).
    /// </summary>
    Task SetArchivedAsync(int campaignId, int characterId, string userId, bool archived, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändrar karaktärens räknare, tillstånd eller sparade slag (B56). Ägaren och GM får ändra.
    /// Returnerar karaktärens nya status, så att sidan kan visa den direkt.
    /// </summary>
    Task<CharacterStatus> EditStatusAsync(int campaignId, int characterId, string userId, CharacterStatusChange change,
        CancellationToken cancellationToken = default);

    /// <summary>Förslag på tillstånd: D&amp;D 5e:s, Bloodied och de som redan används i kampanjen (B56).</summary>
    Task<IReadOnlyList<string>> GetConditionSuggestionsAsync(int campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skapar en kopia med nästa lediga numrerade namn (B68), t.ex. "Goblin 2". GM duplicerar NPC:er, spelare sina egna
    /// karaktärer (inom taket). Returnerar kopians id.
    /// </summary>
    Task<int> DuplicateAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tar bort karaktären. En spelarkaraktär som har skrivit inlägg kan inte tas bort (den kan arkiveras). En NPC kan
    /// alltid tas bort; dess inlägg ligger kvar och visar namnet utan länk och porträtt (B69).
    /// </summary>
    Task DeleteAsync(int campaignId, int characterId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>Karaktärerna på fliken Karaktärer. <see cref="Archived"/> är arkiverade spelarkaraktärer och NPC:er (B68).</summary>
public sealed record CharacterList(
    IReadOnlyList<CharacterSummary> PlayerCharacters,
    IReadOnlyList<CharacterSummary> Npcs,
    IReadOnlyList<CharacterSummary> Archived,
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
    string? Alias = null,
    CharacterStatus? Status = null,
    bool CanDuplicate = false);

/// <summary>
/// Karaktärens räknare, tillstånd och sparade slag (B56). För en NPC får bara GM räknarna och slagen; andra får tomma listor
/// och <see cref="ShowsPrivate"/> = false. Tillstånden ser alla som får se karaktären.
/// </summary>
public sealed record CharacterStatus(
    IReadOnlyList<CounterView> Counters,
    IReadOnlyList<ConditionView> Conditions,
    IReadOnlyList<SavedRollView> SavedRolls,
    bool ShowsPrivate);

/// <summary>En räknare att visa. <see cref="IconUrl"/> är ikonen ur biblioteket (B58), eller null.</summary>
public sealed record CounterView(Guid Uid, string Label, int Current, int Max, int? IconId = null, string? IconUrl = null);

public sealed record ConditionView(Guid Uid, string Name, int? IconId = null, string? IconUrl = null);

public sealed record SavedRollView(Guid Uid, string Label, string Notation, int? IconId = null, string? IconUrl = null)
{
    public static SavedRollView From(SavedRoll roll, string? iconUrl = null) =>
        new(roll.Uid, roll.Label, roll.Notation, roll.IconId, iconUrl);
}

/// <summary>En ändring av karaktärens räknare, tillstånd eller sparade slag (B56).</summary>
public abstract record CharacterStatusChange
{
    public sealed record AddCounter(string? Label, int Current, int Max) : CharacterStatusChange;
    public sealed record UpdateCounter(Guid Uid, string? Label, int Current, int Max) : CharacterStatusChange;

    /// <summary>Byter namn och max (B66); värdet sänks om det är över det nya maxet.</summary>
    public sealed record EditCounter(Guid Uid, string? Label, int Max) : CharacterStatusChange;
    public sealed record AdjustCounter(Guid Uid, int Delta) : CharacterStatusChange;
    public sealed record RemoveCounter(Guid Uid) : CharacterStatusChange;
    public sealed record AddCondition(string? Name) : CharacterStatusChange;
    public sealed record RemoveCondition(Guid Uid) : CharacterStatusChange;
    public sealed record AddSavedRoll(string? Label, string? Notation) : CharacterStatusChange;
    public sealed record UpdateSavedRoll(Guid Uid, string? Label, string? Notation) : CharacterStatusChange;
    public sealed record RemoveSavedRoll(Guid Uid) : CharacterStatusChange;

    /// <summary>Flyttar en räknare eller ett sparat slag i listan (B66), −1 upp och +1 ned.</summary>
    public sealed record Move(Guid Uid, int Offset) : CharacterStatusChange;

    /// <summary>Byter ikon (B58); null tar bort den.</summary>
    public sealed record SetIcon(Guid Uid, int? IconId) : CharacterStatusChange;

    /// <summary>Föreslår en ikon efter namnet igen (B58).</summary>
    public sealed record SuggestIcon(Guid Uid) : CharacterStatusChange;

    /// <summary>
    /// Utför ändringen på karaktären. En ny räknare, ett nytt tillstånd eller ett nytt slag får en ikon som
    /// <paramref name="suggestIcon"/> föreslår efter namnet (B58); förslaget sparas och byts inte av sig själv.
    /// </summary>
    public void ApplyTo(Character character, DateTimeOffset now, Func<string, int?>? suggestIcon = null)
    {
        Guid? added = null;
        switch (this)
        {
            case AddCounter c: added = character.AddCounter(c.Label, c.Current, c.Max, now); break;
            case UpdateCounter c: character.UpdateCounter(c.Uid, c.Label, c.Current, c.Max, now); break;
            case EditCounter c: character.EditCounter(c.Uid, c.Label, c.Max, now); break;
            case AdjustCounter c: character.AdjustCounter(c.Uid, c.Delta, now); break;
            case RemoveCounter c: character.RemoveCounter(c.Uid, now); break;
            case AddCondition c: added = character.AddCondition(c.Name, now); break;
            case RemoveCondition c: character.RemoveCondition(c.Uid, now); break;
            case AddSavedRoll c: added = character.AddSavedRoll(c.Label, c.Notation, now); break;
            case UpdateSavedRoll c: character.UpdateSavedRoll(c.Uid, c.Label, c.Notation, now); break;
            case RemoveSavedRoll c: character.RemoveSavedRoll(c.Uid, now); break;
            case Move c: character.MoveStatusItem(c.Uid, c.Offset, now); break;
            case SetIcon c: character.SetStatusIcon(c.Uid, c.IconId, now); break;
            case SuggestIcon c: character.SetStatusIcon(c.Uid, suggestIcon?.Invoke(character.StatusItemName(c.Uid)), now); break;
            default: throw new ArgumentOutOfRangeException(nameof(CharacterStatusChange));
        }

        if (added is { } uid && suggestIcon is not null)
            character.SetStatusIcon(uid, suggestIcon(character.StatusItemName(uid)), now);
    }
}

/// <summary>
/// Ett val i "Skriv som". <see cref="LastUsedAt"/> är när karaktären senast skrev ett inlägg (B17), och
/// <see cref="SavedRolls"/> karaktärens sparade slag, som kan läggas till i inlägget (B57).
/// </summary>
public sealed record CharacterOption(int Id, string Name, bool IsNpc, string? AvatarUrl, DateTimeOffset? LastUsedAt = null, bool IsHidden = false,
    IReadOnlyList<SavedRollView>? SavedRolls = null);
