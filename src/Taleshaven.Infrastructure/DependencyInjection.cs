using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Media;
using Taleshaven.Core.Moderation;
using Taleshaven.Core.Portraits;
using Taleshaven.Core.Site;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;
using Taleshaven.Core.Users;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Characters;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Dice;
using Taleshaven.Infrastructure.Identity;
using Taleshaven.Infrastructure.Media;
using Taleshaven.Infrastructure.Moderation;
using Taleshaven.Infrastructure.Portraits;
using Taleshaven.Infrastructure.Text;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure;

public static class DependencyInjection
{
    /// <param name="mediaPath">Mapp där uppladdade bilder lagras (utanför wwwroot).</param>
    /// <param name="adminEmails">Sajtens första administratörer (<c>Admin:Emails</c>, B18).</param>
    /// <param name="writeImageManifest">Om bildbibliotekets manifest skrivs om efter ändringar; bara i utvecklingsmiljön (B65).</param>
    public static IServiceCollection AddTaleshavenInfrastructure(
        this IServiceCollection services, string connectionString, string mediaPath, IReadOnlyList<string> adminEmails,
        bool writeImageManifest = false)
    {
        // Fabriken används av tjänsterna (kortlivade contexts, säkert i Blazor Server).
        // AddDbContextFactory registrerar även TaleshavenDbContext som scoped, vilket Identity behöver.
        services.AddDbContextFactory<TaleshavenDbContext>(options => options.UseNpgsql(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignApplicationService, CampaignApplicationService>();
        services.AddScoped<IThreadService, ThreadService>();
        services.AddScoped<IUnreadService, UnreadService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddScoped<IPortraitService, PortraitService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<Taleshaven.Core.Forum.IForumService, Forum.ForumService>();
        services.AddScoped<ISiteRoleService>(provider =>
            new SiteRoleService(provider.GetRequiredService<IDbContextFactory<TaleshavenDbContext>>(), adminEmails));
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
        services.AddSingleton<IDiceRoller, CryptoDiceRoller>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        LocalImageStore.MoveLegacyFolder(mediaPath);
        services.AddSingleton<IImageStore>(new LocalImageStore(mediaPath));
        services.AddSingleton(new ImageLibraryOptions(writeImageManifest));

        return services;
    }

    public static async Task MigrateTaleshavenDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaleshavenDbContext>();
        await db.Database.MigrateAsync();
    }
}
