namespace Taleshaven.Core.Characters;

/// <summary>
/// En karaktär i en kampanj (krav H-1–H-6). Spelare äger sina egna karaktärer; GM:s karaktärer är NPC:er (F3).
/// Spelarkaraktärer har ett formaterat dokument och kan länka till ett externt rollformulär.
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

    public const string UnknownName = "Okänd";

    /// <summary>Namnet som visas för den som inte får se dolda NPC:er.</summary>
    public static string NameForPlayers(string name, bool isHidden, string? alias) => isHidden ? alias ?? UnknownName : name;

    /// <summary>Porträtt ur biblioteket (B19), eller null för initialer. Blir null om porträttet tas bort (B20).</summary>
    public int? PortraitId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

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
            throw new CampaignRuleException("Karaktären måste ha ett namn.");
        if (name.Length > CharacterLimits.NameMaxLength)
            throw new CampaignRuleException($"Namnet får vara högst {CharacterLimits.NameMaxLength} tecken.");

        if (IsNpc)
        {
            // NPC:er har inget dokument, ingen länk och inget regelsystem. Äldre sådana fält lämnas orörda
            // (de visas inte) i stället för att raderas.
            var note = string.IsNullOrWhiteSpace(input.GmNote) ? null : input.GmNote.Trim();
            if (note?.Length > CharacterLimits.GmNoteMaxLength)
                throw new CampaignRuleException($"Anteckningen får vara högst {CharacterLimits.GmNoteMaxLength} tecken.");

            var alias = string.IsNullOrWhiteSpace(input.Alias) ? null : input.Alias.Trim();
            if (alias?.Length > CharacterLimits.NameMaxLength)
                throw new CampaignRuleException($"Aliaset får vara högst {CharacterLimits.NameMaxLength} tecken.");

            Name = name;
            GmNote = note;
            IsHidden = input.Hidden;
            Alias = alias;
            UpdatedAt = now;
            return;
        }

        var sheet = input.Sheet?.Trim() ?? "";
        if (sheet.Length > CharacterLimits.SheetMaxLength)
            throw new CampaignRuleException($"Karaktärsdokumentet får vara högst {CharacterLimits.SheetMaxLength} tecken.");

        var ruleSystem = string.IsNullOrWhiteSpace(input.RuleSystem) ? null : input.RuleSystem.Trim();
        if (ruleSystem?.Length > CharacterLimits.RuleSystemMaxLength)
            throw new CampaignRuleException($"Regelsystemet får vara högst {CharacterLimits.RuleSystemMaxLength} tecken.");

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
            throw new CampaignRuleException("Bara NPC:er kan arkiveras.");

        IsArchived = archived;
    }

    public void SetPortrait(int? portraitId, DateTimeOffset now)
    {
        PortraitId = portraitId;
        UpdatedAt = now;
    }

    // Bara absoluta http(s)-adresser, så att länken aldrig kan bli t.ex. javascript:.
    private static string? ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Trim();
        if (url.Length > CharacterLimits.SheetUrlMaxLength)
            throw new CampaignRuleException($"Länken får vara högst {CharacterLimits.SheetUrlMaxLength} tecken.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new CampaignRuleException("Länken till rollformuläret måste börja med https:// eller http://.");

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
