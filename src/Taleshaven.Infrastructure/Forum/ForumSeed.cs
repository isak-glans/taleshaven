using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Data;

namespace Taleshaven.Infrastructure.Forum;

/// <summary>
/// Forumets starttrådar (B72). Läggs in när appen startar om forumet inte har några trådar och det finns en administratör
/// (från <c>Admin:Emails</c>) att stå som författare. Därefter sköts trådarna på sajten.
/// </summary>
public static class ForumSeed
{
    private sealed record StarterThread(string Title, bool Pinned, bool Locked, string Content);

    private static readonly StarterThread[] Threads =
    [
        new("Welcome to Taleshaven – introduce yourself", Pinned: true, Locked: false, """
            Welcome to **Taleshaven**, a place for telling stories together, one post at a time.

            Say hello in this thread! Tell us a little about yourself, for example:

            - What do you like to play? (D&D, Call of Cthulhu, your own system …)
            - How often do you usually post?
            - What kind of stories do you enjoy – heroic, dark, funny, mysterious?

            You can also add a profile picture and a few words about yourself under your profile.
            """),
        new("FAQ – frequently asked questions", Pinned: true, Locked: true, """
            **How do I join a campaign?**
            Find a campaign on the *Campaigns* page that is *Open for applications* and click *Apply*. The GM decides who gets in.

            **How do I start my own campaign?**
            Click *Create campaign*. You become its GM: you create the threads, approve players and run the NPCs.

            **How do I post as my character?**
            Create a character under the campaign's *Characters* tab. When you write in a thread, choose the character under *Post as*.

            **How do dice rolls work?**
            Click *Add roll* below the text, e.g. `1d20+5`. The dice are rolled by the site when you post, and can't be changed afterwards. Saved rolls on your character can be added with one click.

            **How do I write out of character or hide spoilers?**
            Use `[ooc]…[/ooc]` for out-of-character text and `[spoiler]…[/spoiler]` (or `[spoiler=Title]` on its own lines) for things others may want to skip.

            **Can I post pictures?**
            A link to an image (`https://…png`, `jpg`, `gif` or `webp`) is shown as a small picture below the link.

            **Someone is being rude. What do I do?**
            Use *Report* in the post's "…" menu. The GM and the site's moderators will look at it. See the [rules](rules).

            **How do I delete my account or download my data?**
            Under your profile, *Personal data*.
            """),
        new("Rules", Pinned: true, Locked: true, """
            The site's rules are on the [rules page](rules). In short:

            - Be respectful – no harassment, threats or hate.
            - Agree with your table on dark themes, and use spoilers.
            - Nothing illegal, no spam, and don't share other people's personal information.

            Use *Report* in a post's "…" menu if something breaks the rules.
            """),
        new("Looking for players and campaigns", Pinned: false, Locked: false, """
            Looking for players for your campaign, or for a campaign to join? Post here!

            Tell others what you're looking for, for example the system, the kind of story, how often you post and how many players you need.
            """),
        new("Suggestions and feedback", Pinned: false, Locked: false, """
            Ideas for how Taleshaven could be better? Something you'd like to see? Let us know here.
            """),
        new("Bug reports", Pinned: false, Locked: false, """
            Found something that doesn't work? Describe what you did, what you expected and what happened instead, and which browser or phone you were using.
            """),
    ];

    public static async Task SeedAsync(IServiceProvider services, IReadOnlyList<string> adminEmails, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TaleshavenDbContext>>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        if (await db.Threads.AnyAsync(t => t.CategoryId != null, cancellationToken))
            return;
        var category = await db.ForumCategories.OrderBy(c => c.Position).FirstOrDefaultAsync(cancellationToken);
        if (category is null)
            return;

        string? authorId = null;
        foreach (var email in adminEmails)
        {
            var normalized = email.Trim().ToUpperInvariant();
            authorId = await db.Users.Where(u => u.NormalizedEmail == normalized).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            if (authorId is not null)
                break;
        }
        if (authorId is null)
            return;

        // Trådarna läggs in i omvänd ordning, så att den första hamnar överst bland de senast aktiva.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        for (var i = Threads.Length - 1; i >= 0; i--)
        {
            var starter = Threads[i];
            var time = now.AddSeconds(Threads.Length - i);
            var thread = CampaignThread.CreateForumThread(category.Id, starter.Title, authorId, time);
            thread.SetPinned(starter.Pinned, time);
            thread.SetLocked(starter.Locked, time);
            db.Threads.Add(thread);
            await db.SaveChangesAsync(cancellationToken);
            db.Posts.Add(Post.Create(thread.Id, authorId, starter.Content, time));
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
