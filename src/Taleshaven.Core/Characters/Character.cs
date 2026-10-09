namespace Taleshaven.Core.Characters;

/// <summary>
/// En karaktär i en kampanj (krav H-1–H-6). Spelare äger sina egna karaktärer; GM:s karaktärer är NPC:er (F3).
/// Spelarkaraktärer har ett formaterat dokument och kan länka till ett externt rollformulär. Alla karaktärer kan ha
/// räknare, tillstånd och sparade tärningsslag (B56).
/// NPC:er har bara namn, porträtt och en anteckning som bara GM ser (B15).
/// </summary>
public class Character
{
    private Character() { }

    public int Id { get; private set; }
    public int CampaignId { get; private set; }
    public string OwnerId { get; private set; } = "";
    public bool IsNpc { get; private set; }
    public string Name { get; private set; } = "";

    /// <summary>Beskrivning, HP, resurser, utrustning m.m. i Markdown.</summary>
    public string Sheet { get; private set; } = "";

    /// <summary>Länk till ett fullständigt rollformulär på en annan webbplats (H-4).</summary>
    public string? SheetUrl { get; private set; }

    public string? RuleSystem { get; private set; }

    /// <summary>Anteckning om en NPC som bara GM ser (B15). Alltid null för spelarkaraktärer.</summary>
    public string? GmNote { get; private set; }

    /// <summary>Arkiverade NPC:er göms i "Skriv som" men finns kvar i gamla inlägg (B17). Alltid false för spelarkaraktärer.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Dold NPC (B16): spelarna ser bara <see cref="Alias"/> och en siluett, varken på Karaktärer eller i chatten.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>Namnet spelarna ser medan NPC:n är dold. Utan alias visas <see cref="UnknownName"/>.</summary>
    public string? Alias { get; private set; }

    public const string UnknownName = "Unknown";

    /// <summary>Namnet som visas för den som inte får se dolda NPC:er.</summary>
    public static string NameForPlayers(string name, bool isHidden, string? alias) => isHidden ? alias ?? UnknownName : name;

    /// <summary>Porträtt ur biblioteket (B19), eller null för initialer. Blir null om porträttet tas bort (B20).</summary>
    public int? PortraitId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Räknare som HP och pilar (B56). För NPC:er ser bara GM dem.</summary>
    public List<CharacterCounter> Counters { get; private set; } = [];

    /// <summary>Tillstånd som Poisoned (B56). Synliga för alla som får se karaktären.</summary>
    public List<CharacterCondition> Conditions { get; private set; } = [];

    /// <summary>Sparade tärningsslag (B56) som kan läggas till i ett inlägg (B57). För NPC:er ser bara GM dem.</summary>
    public List<SavedRoll> SavedRolls { get; private set; } = [];

    public static Character Create(int campaignId, string ownerId, bool isNpc, CharacterInput input, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        var character = new Character
        {
            CampaignId = campaignId,
            OwnerId = ownerId,
            IsNpc = isNpc,
            CreatedAt = now,
        };
        character.Update(input, now);
        return character;
    }

    public void Update(CharacterInput input, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(input);

        var name = input.Name?.Trim() ?? "";
        if (name.Length == 0)
            throw new CampaignRuleException("The character needs a name.");
        if (name.Length > CharacterLimits.NameMaxLength)
            throw new CampaignRuleException($"The name can be at most {CharacterLimits.NameMaxLength} characters.");

        if (IsNpc)
        {
            // NPC:er har inget dokument, ingen länk och inget regelsystem. Äldre sådana fält lämnas orörda
            // (de visas inte) i stället för att raderas.
            var note = string.IsNullOrWhiteSpace(input.GmNote) ? null : input.GmNote.Trim();
            if (note?.Length > CharacterLimits.GmNoteMaxLength)
                throw new CampaignRuleException($"The note can be at most {CharacterLimits.GmNoteMaxLength} characters.");

            var alias = string.IsNullOrWhiteSpace(input.Alias) ? null : input.Alias.Trim();
            if (alias?.Length > CharacterLimits.NameMaxLength)
                throw new CampaignRuleException($"The alias can be at most {CharacterLimits.NameMaxLength} characters.");

            Name = name;
            GmNote = note;
            IsHidden = input.Hidden;
            Alias = alias;
            UpdatedAt = now;
            return;
        }

        var sheet = input.Sheet?.Trim() ?? "";
        if (sheet.Length > CharacterLimits.SheetMaxLength)
            throw new CampaignRuleException($"The character sheet can be at most {CharacterLimits.SheetMaxLength} characters.");

        var ruleSystem = string.IsNullOrWhiteSpace(input.RuleSystem) ? null : input.RuleSystem.Trim();
        if (ruleSystem?.Length > CharacterLimits.RuleSystemMaxLength)
            throw new CampaignRuleException($"The rule system can be at most {CharacterLimits.RuleSystemMaxLength} characters.");

        Name = name;
        Sheet = sheet;
        SheetUrl = ValidateUrl(input.SheetUrl);
        RuleSystem = ruleSystem;
        UpdatedAt = now;
    }

    /// <summary>Arkiverar eller återställer en NPC (B17). Spelarkaraktärer kan inte arkiveras.</summary>
    public void SetArchived(bool archived)
    {
        if (!IsNpc)
            throw new CampaignRuleException("Only NPCs can be archived.");

        IsArchived = archived;
    }

    public void SetPortrait(int? portraitId, DateTimeOffset now)
    {
        PortraitId = portraitId;
        UpdatedAt = now;
    }

    public Guid AddCounter(string? label, int current, int max, DateTimeOffset now)
    {
        if (Counters.Count >= CharacterTrackers.MaxCounters)
            throw new CampaignRuleException($"A character can have at most {CharacterTrackers.MaxCounters} counters.");
        var counter = new CharacterCounter(label!, current, max);
        Counters.Add(counter);
        UpdatedAt = now;
        return counter.Uid;
    }

    public void UpdateCounter(Guid uid, string? label, int current, int max, DateTimeOffset now)
    {
        FindCounter(uid).Set(label, current, max);
        UpdatedAt = now;
    }

    /// <summary>Ökar eller minskar räknarens värde, t.ex. −5 HP. Värdet kan gå över max (t.ex. tillfälliga HP).</summary>
    public void AdjustCounter(Guid uid, int delta, DateTimeOffset now)
    {
        FindCounter(uid).Adjust(delta);
        UpdatedAt = now;
    }

    public void RemoveCounter(Guid uid, DateTimeOffset now)
    {
        Counters.Remove(FindCounter(uid));
        UpdatedAt = now;
    }

    /// <summary>Lägger till ett tillstånd. Samma tillstånd två gånger (oavsett stora och små bokstäver) blir ett.</summary>
    public Guid AddCondition(string? name, DateTimeOffset now)
    {
        var text = CharacterTrackers.ValidateLabel(name, "condition");
        if (Conditions.FirstOrDefault(c => string.Equals(c.Name, text, StringComparison.OrdinalIgnoreCase)) is { } existing)
            return existing.Uid;
        if (Conditions.Count >= CharacterTrackers.MaxConditions)
            throw new CampaignRuleException($"A character can have at most {CharacterTrackers.MaxConditions} conditions.");
        var condition = new CharacterCondition(text);
        Conditions.Add(condition);
        UpdatedAt = now;
        return condition.Uid;
    }

    public void RemoveCondition(Guid uid, DateTimeOffset now)
    {
        var condition = Conditions.SingleOrDefault(c => c.Uid == uid)
            ?? throw new CampaignRuleException("The condition doesn't exist any more.");
        Conditions.Remove(condition);
        UpdatedAt = now;
    }

    public Guid AddSavedRoll(string? label, string? notation, Dice.DiceMode mode, DateTimeOffset now)
    {
        if (SavedRolls.Count >= CharacterTrackers.MaxSavedRolls)
            throw new CampaignRuleException($"A character can have at most {CharacterTrackers.MaxSavedRolls} dice rolls.");
        var roll = new SavedRoll(label, notation, mode);
        SavedRolls.Add(roll);
        UpdatedAt = now;
        return roll.Uid;
    }

    public void UpdateSavedRoll(Guid uid, string? label, string? notation, Dice.DiceMode mode, DateTimeOffset now)
    {
        FindSavedRoll(uid).Set(label, notation, mode);
        UpdatedAt = now;
    }

    public void RemoveSavedRoll(Guid uid, DateTimeOffset now)
    {
        SavedRolls.Remove(FindSavedRoll(uid));
        UpdatedAt = now;
    }

    /// <summary>Sätter ikonen (B58) på en räknare, ett tillstånd eller ett sparat slag; null tar bort den.</summary>
    public void SetStatusIcon(Guid uid, int? iconId, DateTimeOffset now)
    {
        if (Counters.SingleOrDefault(c => c.Uid == uid) is { } counter)
            counter.SetIcon(iconId);
        else if (Conditions.SingleOrDefault(c => c.Uid == uid) is { } condition)
            condition.SetIcon(iconId);
        else if (SavedRolls.SingleOrDefault(r => r.Uid == uid) is { } roll)
            roll.SetIcon(iconId);
        else
            throw new CampaignRuleException("The item doesn't exist any more.");
        UpdatedAt = now;
    }

    /// <summary>Namnet på en räknare, ett tillstånd eller ett sparat slag, t.ex. för att föreslå en ikon.</summary>
    public string StatusItemName(Guid uid) =>
        Counters.SingleOrDefault(c => c.Uid == uid)?.Label
        ?? Conditions.SingleOrDefault(c => c.Uid == uid)?.Name
        ?? SavedRolls.SingleOrDefault(r => r.Uid == uid)?.Label
        ?? throw new CampaignRuleException("The item doesn't exist any more.");

    private CharacterCounter FindCounter(Guid uid) =>
        Counters.SingleOrDefault(c => c.Uid == uid) ?? throw new CampaignRuleException("The counter doesn't exist any more.");

    private SavedRoll FindSavedRoll(Guid uid) =>
        SavedRolls.SingleOrDefault(r => r.Uid == uid) ?? throw new CampaignRuleException("The dice roll doesn't exist any more.");

    // Bara absoluta http(s)-adresser, så att länken aldrig kan bli t.ex. javascript:.
    private static string? ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();
        if (url.Length > CharacterLimits.SheetUrlMaxLength)
            throw new CampaignRuleException($"The link can be at most {CharacterLimits.SheetUrlMaxLength} characters.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new CampaignRuleException("The character sheet link must start with https:// or http://.");

        return uri.ToString();
    }
}

/// <summary>Uppgifter för att skapa eller ändra en karaktär. För NPC:er används bara <see cref="Name"/>, <see cref="GmNote"/>,
/// <see cref="Hidden"/> och <see cref="Alias"/>.</summary>
public sealed record CharacterInput(
    string? Name, string? Sheet, string? SheetUrl, string? RuleSystem, string? GmNote = null, bool Hidden = false, string? Alias = null);

public static class CharacterLimits
{
    public const int NameMaxLength = 60;
    public const int SheetMaxLength = 10_000;
    public const int SheetUrlMaxLength = 500;
    public const int RuleSystemMaxLength = 60;
    public const int GmNoteMaxLength = 2_000;
}
