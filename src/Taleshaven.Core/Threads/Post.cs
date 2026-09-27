using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

/// <summary>Ett inlägg i en tråd (B25, B30). Inlägg är bestående delar av berättelsen, inte flyktiga chattmeddelanden.</summary>
public class Post
{
    private Post() { }

    public long Id { get; private set; }
    public int ThreadId { get; private set; }
    public string AuthorId { get; private set; } = "";

    /// <summary>Inläggets text i Markdown, med tärningsslagen som referenser <c>[dice:N]</c> (B31). Renderas och saneras vid visning.</summary>
    public string Content { get; private set; } = "";

    /// <summary>Karaktären inlägget är skrivet som (Story-trådar), eller null om det är skrivet som användaren själv (B29).</summary>
    public int? CharacterId { get; private set; }

    /// <summary>Tärningsslagen i texten (B31), i samma ordning som referenserna <c>[dice:1]</c>, <c>[dice:2]</c> …</summary>
    public List<DiceRoll> Rolls { get; private set; } = [];

    /// <summary>Inlägget som det här svarar på (B30), eller null.</summary>
    public long? ReplyToPostId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>När inlägget senast redigerades, eller null om det aldrig har redigerats (B12).</summary>
    public DateTimeOffset? EditedAt { get; private set; }

    /// <summary>
    /// När inlägget togs bort (B30). Borttagningen är mjuk: inlägget ligger kvar på sin plats som "This post was deleted.",
    /// och texten finns kvar i databasen som historik men visas aldrig.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedById { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>Inlägg med tärningsslag kan bara tas bort av GM (B31).</summary>
    public bool HasRolls => Rolls.Count > 0;

    /// <summary>
    /// Skapar ett inlägg. Taggar som <c>[dice]1d20+3[/dice]</c> slås här med <paramref name="roller"/> (B31);
    /// utan tärningsslagare får texten inte innehålla taggar.
    /// </summary>
    public static Post Create(int threadId, string authorId, string? content, DateTimeOffset now,
        int? characterId = null, long? replyToPostId = null, IDiceRoller? roller = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);

        var text = ValidateContent(content);
        List<DiceRoll> rolls = [];
        if (InlineDice.ContainsTags(text))
        {
            if (roller is null)
                throw new InvalidOperationException("Inlägget innehåller tärningar men ingen tärningsslagare angavs.");
            (text, rolls) = InlineDice.RollAll(text, roller);
        }

        return new Post
        {
            ThreadId = threadId,
            AuthorId = authorId,
            Content = text,
            CharacterId = characterId,
            ReplyToPostId = replyToPostId,
            Rolls = rolls,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Byter inläggets text. Returnerar den tidigare versionen som ska sparas som historik (F6).
    /// Tärningsslag kan varken tas bort eller läggas till vid redigering (B31).
    /// </summary>
    public PostRevision Edit(string? content, DateTimeOffset now)
    {
        if (IsDeleted)
            throw new CampaignRuleException("A deleted post can't be edited.");

        var text = ValidateContent(content);
        InlineDice.EnsureValidEdit(text, Rolls.Count);

        var revision = new PostRevision(Id, Content, EditedAt ?? CreatedAt, now);
        Content = text;
        EditedAt = now;
        return revision;
    }

    /// <summary>Tar bort inlägget mjukt (B30). Vem som får ta bort avgörs av <see cref="Campaigns.CampaignPermissions.CanDeletePost"/>.</summary>
    public void Delete(string deletedById, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedById);
        if (IsDeleted)
            throw new CampaignRuleException("The post is already deleted.");

        DeletedAt = now;
        DeletedById = deletedById;
    }

    private static string ValidateContent(string? content)
    {
        var text = content?.Trim() ?? "";
        if (text.Length == 0)
            throw new CampaignRuleException("The post is empty.");
        if (text.Length > ThreadLimits.PostMaxLength)
            throw new CampaignRuleException($"The post can be at most {ThreadLimits.PostMaxLength} characters.");
        return text;
    }
}

/// <summary>En tidigare version av ett redigerat inlägg.</summary>
public class PostRevision
{
    private PostRevision() { }

    internal PostRevision(long postId, string content, DateTimeOffset writtenAt, DateTimeOffset replacedAt)
    {
        PostId = postId;
        Content = content;
        WrittenAt = writtenAt;
        ReplacedAt = replacedAt;
    }

    public long Id { get; private set; }
    public long PostId { get; private set; }

    /// <summary>Texten som gällde innan redigeringen.</summary>
    public string Content { get; private set; } = "";

    /// <summary>När den här versionen skrevs (inlägget skapades eller redigerades förra gången).</summary>
    public DateTimeOffset WrittenAt { get; private set; }

    /// <summary>När versionen ersattes av en redigering.</summary>
    public DateTimeOffset ReplacedAt { get; private set; }
}
