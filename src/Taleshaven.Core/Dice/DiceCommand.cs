namespace Taleshaven.Core.Dice;

/// <summary>
/// Tärningskommandot i chatten: <c>/roll 2d6+3 attack the orc</c> (F13, B24).
/// Text efter notationen blir en valfri beskrivning av kastet.
/// </summary>
public static class DiceCommand
{
    private const string Prefix = "/roll";

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
        var isCommand = trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && (trimmed.Length == Prefix.Length || char.IsWhiteSpace(trimmed[Prefix.Length]));

        if (!isCommand)
            return false;

        var arguments = trimmed[Prefix.Length..].Trim();
        var parts = arguments.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "Say which dice to roll, e.g. /roll 2d6+3.";
            return true;
        }

        if (!DiceNotation.TryParse(parts[0], out notation, out error))
            return true;

        label = parts.Length > 1 ? parts[1].Trim() : null;
        if (label?.Length > DiceRoll.LabelMaxLength)
            error = $"The description can be at most {DiceRoll.LabelMaxLength} characters.";

        return true;
    }
}
