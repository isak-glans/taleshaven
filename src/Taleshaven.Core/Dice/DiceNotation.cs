using System.Text.RegularExpressions;

namespace Taleshaven.Core.Dice;

/// <summary>
/// Tärningsnotation som <c>2d6+3</c>, <c>1d20</c>, <c>d100</c> eller <c>3d8-2</c> (krav T-1, T-2, T-6).
/// </summary>
public readonly partial record struct DiceNotation
{
    public static readonly IReadOnlyList<int> AllowedSides = [4, 6, 8, 10, 12, 20, 100];
    public const int MaxCount = 50;
    public const int MaxModifier = 999;

    public DiceNotation(int count, int sides, int modifier = 0)
    {
        if (count is < 1 or > MaxCount)
            throw new CampaignRuleException($"Antalet tärningar måste vara mellan 1 och {MaxCount}.");
        if (!AllowedSides.Contains(sides))
            throw new CampaignRuleException($"Tärningen d{sides} stöds inte. Använd {string.Join(", ", AllowedSides.Select(s => $"d{s}"))}.");
        if (Math.Abs(modifier) > MaxModifier)
            throw new CampaignRuleException($"Modifieraren måste vara mellan -{MaxModifier} och +{MaxModifier}.");

        Count = count;
        Sides = sides;
        Modifier = modifier;
    }

    public int Count { get; }
    public int Sides { get; }
    public int Modifier { get; }

    /// <summary>Tolkar notationen. Mellanslag ignoreras och "D" fungerar som "d".</summary>
    public static bool TryParse(string? input, out DiceNotation notation, out string? error)
    {
        notation = default;
        error = null;

        var match = NotationPattern().Match((input ?? "").Replace(" ", ""));
        if (!match.Success)
        {
            error = "Ogiltig tärning. Skriv t.ex. 2d6+3, 1d20 eller d100.";
            return false;
        }

        var count = match.Groups["count"].Success ? int.Parse(match.Groups["count"].Value) : 1;
        var sides = int.Parse(match.Groups["sides"].Value);
        var modifier = match.Groups["modifier"].Success ? int.Parse(match.Groups["modifier"].Value) : 0;

        try
        {
            notation = new DiceNotation(count, sides, modifier);
            return true;
        }
        catch (CampaignRuleException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public override string ToString() => Modifier switch
    {
        > 0 => $"{Count}d{Sides}+{Modifier}",
        < 0 => $"{Count}d{Sides}{Modifier}",
        _ => $"{Count}d{Sides}",
    };

    // Siffrorna begränsas i längd så att int.Parse aldrig kan svämma över.
    [GeneratedRegex(@"^(?<count>\d{1,3})?[dD](?<sides>\d{1,3})(?<modifier>[+-]\d{1,4})?$")]
    private static partial Regex NotationPattern();
}
