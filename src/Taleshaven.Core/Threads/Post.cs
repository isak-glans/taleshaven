using Taleshaven.Core.Dice;

namespace Taleshaven.Core.Threads;

/// <summary>Ett inlägg i en tråd (B25, B30). Inlägg är bestående delar av berättelsen, inte flyktiga chattmeddelanden.</summary>
public class Post
{
    private Post() { }

    public long Id { get; private set; }
    public int ThreadId { get; private set; }
    public string AuthorId { get; private set; } = "";

    /// <summary>Inläggets text i Markdown. Renderas och saneras vid visning. Får vara tom om inlägget har tärningsslag (B42).</summary>
    public string Content { get; private set; } = "";

    /// <summary>Karaktären inlägget är skrivet som (Story-trådar), eller null om det är skrivet som användaren själv (B29).</summary>
    public int? CharacterId { get; private set; }

    /// <summary>
    /// Namnet på en NPC som har tagits bort (B69). Inlägget ligger kvar och visar namnet, men utan länk och porträtt;
    /// <see cref="CharacterId"/> är då null. Var NPC:n dold är det namnet spelarna såg (aliaset eller "Unknown").
    /// </summary>
    public string? DeletedCharacterName { get; private set; }

    /// <summary>Tärningsslagen (B42) i den ordning skribenten lade till dem. De visas som en lista under texten.</summary>
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

    /// <summary>Inlägg med tärningsslag kan bara tas bort av GM (B31, B42).</summary>
    public bool HasRolls => Rolls.Count > 0;

    /// <summary>
    /// Skapar ett inlägg. Slagen i <paramref name="rolls"/> slås här med <paramref name="roller"/> (B42), alltså på
    /// servern när inlägget publiceras. Ett inlägg med slag får sakna text.
    /// </summary>
    public static Post Create(int threadId, string authorId, string? content, DateTimeOffset now,
        int? characterId = null, long? replyToPostId = null, IReadOnlyList<RollRequest>? rolls = null, IDiceRoller? roller = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorId);

        rolls ??= [];
        if (rolls.Count > ThreadLimits.MaxRollsPerPost)
            throw new CampaignRuleException($"A post can have at most {ThreadLimits.MaxRollsPerPost} dice rolls.");
        if (rolls.Count > 0 && roller is null)
            throw new InvalidOperationException("Inlägget har tärningsslag men ingen tärningsslagare angavs.");

        var text = ValidateContent(content, allowEmpty: rolls.Count > 0);
        var rolled = rolls.Select(request => DiceRoll.Roll(request, roller!)).ToList();

        return new Post
        {
            ThreadId = threadId,
            AuthorId = authorId,
            Content = text,
            CharacterId = characterId,
            ReplyToPostId = replyToPostId,
            Rolls = rolled,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Byter inläggets text. Returnerar den tidigare versionen som ska sparas som historik (F6).
    /// Bara texten ändras; tärningsslagen kan varken ändras, tas bort eller läggas till (B31, B42).
    /// </summary>
    public PostRevision Edit(string? content, DateTimeOffset now)
    {
        if (IsDeleted)
            throw new CampaignRuleException("A deleted post can't be edited.");

        var text = ValidateContent(content, allowEmpty: HasRolls);

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

    private static string ValidateContent(string? content, bool allowEmpty)
    {
        var text = content?.Trim() ?? "";
        if (text.Length == 0 && !allowEmpty)
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
