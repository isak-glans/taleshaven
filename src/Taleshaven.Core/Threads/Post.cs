using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

public class Post
{
    private Post() { }

    public long Id { get; private set; }
    public int ThreadId { get; private set; }
    public string AuthorId { get; private set; } = "";

    /// <summary>Inläggets text i Markdown. Renderas och saneras vid visning. För tärningskast en läsbar sammanfattning.</summary>
    public string Content { get; private set; } = "";

    /// <summary>Karaktären inlägget är skrivet som (RPG), eller null om det är skrivet som användaren själv.</summary>
    public int? CharacterId { get; private set; }

    /// <summary>Tärningskastet om inlägget är ett kast. Sådana inlägg får aldrig redigeras (T-5).</summary>
    public DiceRoll? Roll { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Post Create(int threadId, string authorId, string? content, DateTimeOffset now, int? characterId = null)
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
            CharacterId = characterId,
            CreatedAt = now,
        };
    }

    public static Post CreateDiceRoll(int threadId, string authorId, DiceRoll roll, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);
        ArgumentNullException.ThrowIfNull(roll);

        return new Post
        {
            ThreadId = threadId,
            AuthorId = authorId,
            Content = roll.ToText(),
            Roll = roll,
            CreatedAt = now,
        };
    }
}
