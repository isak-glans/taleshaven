using Taleshaven.Core.Chronicle;

namespace Taleshaven.Web.Components.Playroom;

/// <summary>Adresser i krönikan, så att alla sidor länkar på samma sätt.</summary>
public static class ChronicleLinks
{
    public static string Chronicle(int campaignId) => $"campaigns/{campaignId}/chronicle";

    public static string Page(int campaignId, int page) => $"campaigns/{campaignId}/chronicle?sida={page}";

    public static string Chapter(int campaignId, int number) => Page(campaignId, ChronicleLimits.PageOf(number));

    public static string NewChapter(int campaignId) => $"campaigns/{campaignId}/chronicle/new";

    public static string EditChapter(int campaignId, int chapterId) => $"campaigns/{campaignId}/chronicle/{chapterId}/edit";
}
