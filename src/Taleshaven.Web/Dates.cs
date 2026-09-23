using System.Globalization;

namespace Taleshaven.Web;

/// <summary>
/// Visar tider i svensk tid oavsett serverns tidszon (t.ex. UTC i molnet).
/// </summary>
public static class Dates
{
    private static readonly TimeZoneInfo Sweden = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
    private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

    public static string Date(DateTimeOffset value) => ToSwedishTime(value).ToString("d MMM yyyy", Swedish);

    public static string DateTime(DateTimeOffset value) => ToSwedishTime(value).ToString("d MMM yyyy HH:mm", Swedish);

    public static string Time(DateTimeOffset value) => ToSwedishTime(value).ToString("HH:mm", Swedish);

    /// <summary>Kalenderdagen i svensk tid, för att avgöra när en ny dag börjar i chatten.</summary>
    public static DateOnly Day(DateTimeOffset value) => DateOnly.FromDateTime(ToSwedishTime(value).DateTime);

    /// <summary>Rubrik för en dag i chatten: "Idag", "Igår", "Tisdag 23 september" (med år om det inte är i år).</summary>
    public static string DayHeading(DateTimeOffset value, DateTimeOffset now)
    {
        var day = Day(value);
        var today = Day(now);

        if (day == today)
            return "Idag";
        if (day == today.AddDays(-1))
            return "Igår";

        var format = day.Year == today.Year ? "dddd d MMMM" : "dddd d MMMM yyyy";
        var text = day.ToString(format, Swedish);
        return char.ToUpper(text[0], Swedish) + text[1..];
    }

    private static DateTimeOffset ToSwedishTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Sweden);
}
