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

    private static DateTimeOffset ToSwedishTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Sweden);
}
