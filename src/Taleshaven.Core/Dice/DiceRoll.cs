namespace Taleshaven.Core.Dice;

/// <summary>Ger ett tärningsresultat mellan 1 och <paramref name="sides"/>. Implementeras med kryptografisk slump på servern (T-5).</summary>
public interface IDiceRoller
{
    int RollDie(int sides);
}

// Värdena lagras i databasen (inläggens slag). Ändra inte befintliga värden.
public enum DiceMode
{
    Normal = 0,

    /// <summary>Två d20, den högsta räknas (B42).</summary>
    Advantage = 1,

    /// <summary>Två d20, den lägsta räknas (B42).</summary>
    Disadvantage = 2,
}

/// <summary>
/// Ett slag som skribenten har lagt till i ett inlägg men som inte är slaget än (B42). <see cref="IconId"/> är ikonen ur
/// biblioteket (B60), t.ex. från ett sparat slag; utan ikon föreslås en efter beskrivningen.
/// </summary>
public sealed record RollRequest(string? Notation, string? Label = null, DiceMode Mode = DiceMode.Normal, int? IconId = null);

/// <summary>
/// Ett genomfört tärningskast. Lagras tillsammans med inlägget det hör till och kan inte ändras (T-5).
/// Med fördel eller nackdel (B42) innehåller <see cref="Results"/> båda tärningarna, och den som räknas ingår i <see cref="Total"/>.
/// </summary>
public class DiceRoll
{
    public const int LabelMaxLength = 100;

    private DiceRoll() { }

    public string Notation { get; private set; } = "";
    public string? Label { get; private set; }
    public int Count { get; private set; }
    public int Sides { get; private set; }
    public int Modifier { get; private set; }
    public DiceMode Mode { get; private set; }
    public List<int> Results { get; private set; } = [];
    public int Total { get; private set; }

    /// <summary>Ikonen som visas framför slaget (B60), eller null för 🎲. Sparas med slaget och ändras inte.</summary>
    public int? IconId { get; private set; }

    /// <summary>Fördel och nackdel gäller bara en ensam d20, t.ex. 1d20+5.</summary>
    public static bool SupportsMode(DiceNotation notation) => notation is { Count: 1, Sides: 20 };

    /// <summary>Tolkar och slår ett slag som skribenten har lagt till. Kastar <see cref="CampaignRuleException"/> vid fel.</summary>
    public static DiceRoll Roll(RollRequest request, IDiceRoller roller)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!DiceNotation.TryParse(request.Notation, out var notation, out var error))
            throw new CampaignRuleException(string.IsNullOrWhiteSpace(request.Notation) ? "Enter the dice to roll, e.g. 1d20+5." : error!);
        var roll = Roll(notation, request.Label, roller, request.Mode);
        roll.IconId = request.IconId;
        return roll;
    }

    public static DiceRoll Roll(DiceNotation notation, string? label, IDiceRoller roller, DiceMode mode = DiceMode.Normal)
    {
        ArgumentNullException.ThrowIfNull(roller);
        if (notation.Count == 0)
            throw new ArgumentException("Notationen är inte angiven.", nameof(notation));
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (mode != DiceMode.Normal && !SupportsMode(notation))
            throw new CampaignRuleException($"Advantage and disadvantage only work with a single d20, not {notation}.");

        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        if (label?.Length > LabelMaxLength)
            throw new CampaignRuleException($"The description can be at most {LabelMaxLength} characters.");

        var dice = mode == DiceMode.Normal ? notation.Count : 2;
        var results = new List<int>(dice);
        for (var i = 0; i < dice; i++)
        {
            var value = roller.RollDie(notation.Sides);
            if (value < 1 || value > notation.Sides)
                throw new InvalidOperationException($"Tärningen gav {value}, utanför 1–{notation.Sides}.");
            results.Add(value);
        }

        var counted = mode switch
        {
            DiceMode.Advantage => results.Max(),
            DiceMode.Disadvantage => results.Min(),
            _ => results.Sum(),
        };

        return new DiceRoll
        {
            Notation = notation.ToString(),
            Label = label,
            Count = notation.Count,
            Sides = notation.Sides,
            Modifier = notation.Modifier,
            Mode = mode,
            Results = results,
            Total = counted + notation.Modifier,
        };
    }
}
