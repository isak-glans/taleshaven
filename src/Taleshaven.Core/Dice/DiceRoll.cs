namespace Taleshaven.Core.Dice;

/// <summary>Ger ett tärningsresultat mellan 1 och <paramref name="sides"/>. Implementeras med kryptografisk slump på servern (T-5).</summary>
public interface IDiceRoller
{
    int RollDie(int sides);
}

/// <summary>
/// Ett genomfört tärningskast. Lagras tillsammans med inlägget det visas som och kan inte ändras (T-5).
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
    public List<int> Results { get; private set; } = [];
    public int Total { get; private set; }

    public static DiceRoll Roll(DiceNotation notation, string? label, IDiceRoller roller)
    {
        ArgumentNullException.ThrowIfNull(roller);
        if (notation.Count == 0)
            throw new ArgumentException("Notationen är inte angiven.", nameof(notation));

        label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        if (label?.Length > LabelMaxLength)
            throw new CampaignRuleException($"Beskrivningen får vara högst {LabelMaxLength} tecken.");

        var results = new List<int>(notation.Count);
        for (var i = 0; i < notation.Count; i++)
        {
            var value = roller.RollDie(notation.Sides);
            if (value < 1 || value > notation.Sides)
                throw new InvalidOperationException($"Tärningen gav {value}, utanför 1–{notation.Sides}.");
            results.Add(value);
        }

        return new DiceRoll
        {
            Notation = notation.ToString(),
            Label = label,
            Count = notation.Count,
            Sides = notation.Sides,
            Modifier = notation.Modifier,
            Results = results,
            Total = results.Sum() + notation.Modifier,
        };
    }

    /// <summary>Kastet som läsbar text, t.ex. "🎲 2d6+3 (anfall): [4, 5] + 3 = 12".</summary>
    public string ToText()
    {
        var label = Label is null ? "" : $" ({Label})";
        var modifier = Modifier switch
        {
            > 0 => $" + {Modifier}",
            < 0 => $" - {-Modifier}",
            _ => "",
        };
        return $"🎲 {Notation}{label}: [{string.Join(", ", Results)}]{modifier} = {Total}";
    }
}
