namespace Taleshaven.Core.Dice;

/// <summary>
/// Tärningskommandot i chatten: <c>/slå 2d6+3 anfall mot orchen</c> eller <c>/roll 1d20</c> (arbetsförslag F13).
/// Text efter notationen blir en valfri beskrivning av kastet.
/// </summary>
public static class DiceCommand
{
    private static readonly string[] Prefixes = ["/slå", "/roll"];

    /// <summary>
    /// Avgör om texten är ett tärningskommando. Returnerar false för vanlig text.
    /// Är det ett kommando sätts antingen <paramref name="notation"/> eller <paramref name="error"/>.
    /// </summary>
    public static bool IsCommand(string? text, out DiceNotation notation, out string? label, out string? error)
    {
        notation = default;
        label = null;
        error = null;

        var trimmed = text?.Trim() ?? "";
        var prefix = Prefixes.FirstOrDefault(p =>
            trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase)
            && (trimmed.Length == p.Length || char.IsWhiteSpace(trimmed[p.Length])));

        if (prefix is null)
            return false;

        var arguments = trimmed[prefix.Length..].Trim();
        var parts = arguments.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "Ange vilken tärning du vill slå, t.ex. /slå 2d6+3.";
            return true;
        }

        if (!DiceNotation.TryParse(parts[0], out notation, out error))
            return true;

        label = parts.Length > 1 ? parts[1].Trim() : null;
        if (label?.Length > DiceRoll.LabelMaxLength)
            error = $"Beskrivningen får vara högst {DiceRoll.LabelMaxLength} tecken.";

        return true;
    }
}
