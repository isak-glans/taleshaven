namespace Taleshaven.Web.Components.Playroom;

/// <summary>Adresser till trådarna (B25–B33). Kampanjens startsida är trådlistan.</summary>
public static class ThreadLinks
{
    public static string Threads(int campaignId) => $"campaigns/{campaignId}";

    /// <summary>Tråden öppnas vid första olästa inlägget, annars på sista sidan (B28).</summary>
    public static string Thread(int campaignId, int threadId) => $"campaigns/{campaignId}/threads/{threadId}";

    public static string Page(int campaignId, int threadId, int page) => $"{Thread(campaignId, threadId)}?page={page}";

    /// <summary>Fast länk till ett inlägg (B30); sidan räknas ut.</summary>
    public static string Post(int campaignId, int threadId, long postId) => $"{Thread(campaignId, threadId)}?post={postId}";

    public static string Latest(int campaignId, int threadId) => $"{Thread(campaignId, threadId)}?page=last";

    public static string New(int campaignId) => $"campaigns/{campaignId}/threads/new";

    public static string Edit(int campaignId, int threadId) => $"{Thread(campaignId, threadId)}/edit";

    public static string Complete(int campaignId, int threadId) => $"{Thread(campaignId, threadId)}/complete";

    public static string Chronicle(int campaignId, int threadId) => $"{Thread(campaignId, threadId)}/chronicle";
}
