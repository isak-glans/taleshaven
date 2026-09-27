using System.Globalization;

namespace Taleshaven.Web;

/// <summary>
/// Visar tider i svensk tid oavsett serverns tidszon (t.ex. UTC i molnet), med engelska datum (B24).
/// </summary>
public static class Dates
{
    private static readonly TimeZoneInfo Sweden = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    public static string Date(DateTimeOffset value) => ToSwedishTime(value).ToString("d MMM yyyy", English);

    public static string DateTime(DateTimeOffset value) => ToSwedishTime(value).ToString("d MMM yyyy HH:mm", English);

    public static string Time(DateTimeOffset value) => ToSwedishTime(value).ToString("HH:mm", English);

    /// <summary>Kort relativ tid, t.ex. "just now", "5 minutes ago", "2 hours ago", "yesterday"; äldre än en vecka som datum.</summary>
    public static string Relative(DateTimeOffset value, DateTimeOffset now)
    {
        var age = now - value;
        if (age < TimeSpan.FromMinutes(1))
            return "just now";
        if (age < TimeSpan.FromHours(1))
            return Plural((int)age.TotalMinutes, "minute") + " ago";
        if (age < TimeSpan.FromDays(1))
            return Plural((int)age.TotalHours, "hour") + " ago";
        if (Day(value) == Day(now).AddDays(-1))
            return "yesterday";
        if (age < TimeSpan.FromDays(7))
            return Plural((int)Math.Ceiling(age.TotalDays), "day") + " ago";
        return Date(value);
    }

    private static string Plural(int count, string unit) => count == 1 ? $"1 {unit}" : $"{count} {unit}s";

    /// <summary>Kalenderdagen i svensk tid, för att avgöra när en ny dag börjar i chatten.</summary>
    public static DateOnly Day(DateTimeOffset value) => DateOnly.FromDateTime(ToSwedishTime(value).DateTime);

    /// <summary>Rubrik för en dag i chatten: "Today", "Yesterday", "Tuesday 23 September" (med år om det inte är i år).</summary>
    public static string DayHeading(DateTimeOffset value, DateTimeOffset now)
    {
        var day = Day(value);
        var today = Day(now);

        if (day == today)
            return "Today";
        if (day == today.AddDays(-1))
            return "Yesterday";

        var format = day.Year == today.Year ? "dddd d MMMM" : "dddd d MMMM yyyy";
        return day.ToString(format, English);
    }

    private static DateTimeOffset ToSwedishTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Sweden);
}
