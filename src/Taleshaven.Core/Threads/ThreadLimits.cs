namespace Taleshaven.Core.Threads;

public static class ThreadLimits
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 500;
    public const int PostMaxLength = 10_000;

    /// <summary>Antal inlägg som visas först och som hämtas per "Ladda äldre inlägg".</summary>
    public const int PostsPageSize = 20;
}
