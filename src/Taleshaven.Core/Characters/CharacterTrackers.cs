using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Characters;

/// <summary>En räknare på en karaktär (B56), t.ex. "HP 28 / 38" eller "Arrows 12 / 20".</summary>
public class CharacterCounter
{
    private CharacterCounter() { }

    internal CharacterCounter(string label, int current, int max)
    {
        Uid = Guid.CreateVersion7();
        Set(label, current, max);
    }

    public Guid Uid { get; private set; }
    public string Label { get; private set; } = "";
    public int Current { get; private set; }
    public int Max { get; private set; }

    /// <summary>Ikon ur biblioteket (B58), eller null.</summary>
    public int? IconId { get; private set; }

    internal void SetIcon(int? iconId) => IconId = iconId;

    internal void Set(string? label, int current, int max)
    {
        Label = CharacterTrackers.ValidateLabel(label, "counter");
        if (max is < 1 or > CharacterTrackers.MaxCounterValue)
            throw new CampaignRuleException($"The maximum must be between 1 and {CharacterTrackers.MaxCounterValue}.");
        Current = CharacterTrackers.ClampCurrent(current);
        Max = max;
    }

    internal void Adjust(int delta) => Current = CharacterTrackers.ClampCurrent(Current + delta);
}

/// <summary>Ett tillstånd på en karaktär (B56), t.ex. "Poisoned" eller "Bloodied".</summary>
public class CharacterCondition
{
    private CharacterCondition() { }

    internal CharacterCondition(string name)
    {
        Uid = Guid.CreateVersion7();
        Name = name;
    }

    public Guid Uid { get; private set; }
    public string Name { get; private set; } = "";

    /// <summary>Ikon ur biblioteket (B58), eller null.</summary>
    public int? IconId { get; private set; }

    internal void SetIcon(int? iconId) => IconId = iconId;
}

/// <summary>Ett sparat tärningsslag på en karaktär (B56), t.ex. "Shortsword 1d20+5". Slås först i ett inlägg (B57).</summary>
public class SavedRoll
{
    private SavedRoll() { }

    internal SavedRoll(string? label, string? notation, DiceMode mode)
    {
        Uid = Guid.CreateVersion7();
        Set(label, notation, mode);
    }

    public Guid Uid { get; private set; }
    public string Label { get; private set; } = "";
    public string Notation { get; private set; } = "";
    public DiceMode Mode { get; private set; }

    /// <summary>Ikon ur biblioteket (B58), eller null.</summary>
    public int? IconId { get; private set; }

    internal void SetIcon(int? iconId) => IconId = iconId;

    internal void Set(string? label, string? notation, DiceMode mode)
    {
        Label = CharacterTrackers.ValidateLabel(label, "dice roll");
        if (!DiceNotation.TryParse(notation, out var parsed, out var error))
            throw new CampaignRuleException(string.IsNullOrWhiteSpace(notation) ? "Enter the dice, e.g. 1d20+5." : error!);
        if (!Enum.IsDefined(mode))
            throw new CampaignRuleException("Unknown roll mode.");
        if (mode != DiceMode.Normal && !DiceRoll.SupportsMode(parsed))
            throw new CampaignRuleException("Advantage and disadvantage only work with a single d20.");
        Notation = parsed.ToString();
        Mode = mode;
    }
}

/// <summary>Gränser och förslag för räknare, tillstånd och sparade slag (B56).</summary>
public static class CharacterTrackers
{
    public const int LabelMaxLength = 40;
    public const int MaxCounters = 20;
    public const int MaxConditions = 20;
    public const int MaxSavedRolls = 30;
    public const int MaxCounterValue = 9_999;

    /// <summary>
    /// Förslag på tillstånd: D&amp;D 5e:s tillstånd och "Bloodied", som visar att varelsen är svårt skadad.
    /// Tillstånd som redan används i kampanjen föreslås också.
    /// </summary>
    public static readonly IReadOnlyList<string> StandardConditions =
    [
        "Blinded", "Bloodied", "Charmed", "Deafened", "Exhaustion", "Frightened", "Grappled", "Incapacitated", "Invisible",
        "Paralyzed", "Petrified", "Poisoned", "Prone", "Restrained", "Stunned", "Unconscious",
    ];

    internal static string ValidateLabel(string? label, string what)
    {
        var text = label?.Trim() ?? "";
        if (text.Length == 0)
            throw new CampaignRuleException($"The {what} needs a name.");
        if (text.Length > LabelMaxLength)
            throw new CampaignRuleException($"The name can be at most {LabelMaxLength} characters.");
        return text;
    }

    internal static int ClampCurrent(int value) => Math.Clamp(value, -MaxCounterValue, MaxCounterValue);
}
