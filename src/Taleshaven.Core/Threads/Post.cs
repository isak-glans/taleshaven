namespace Taleshaven.Core.Threads;

public class Post
{
    private Post() { }

    public long Id { get; private set; }
    public int ThreadId { get; private set; }
    public string AuthorId { get; private set; } = "";

    /// <summary>Inläggets text i Markdown. Renderas och saneras vid visning.</summary>
    public string Content { get; private set; } = "";

    public DateTimeOffset CreatedAt { get; private set; }

    public static Post Create(int threadId, string authorId, string? content, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);

        content = content?.Trim() ?? "";
        if (content.Length == 0)
            throw new CampaignRuleException("Inlägget är tomt.");
        if (content.Length > ThreadLimits.PostMaxLength)
            throw new CampaignRuleException($"Inlägget får vara högst {ThreadLimits.PostMaxLength} tecken.");

        return new Post
        {
            ThreadId = threadId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = now,
        };
    }
}
