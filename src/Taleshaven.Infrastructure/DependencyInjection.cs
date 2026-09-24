using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Chronicle;
using Taleshaven.Core.Dice;
using Taleshaven.Core.Media;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Characters;
using Taleshaven.Infrastructure.Chronicle;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Dice;
using Taleshaven.Infrastructure.Media;
using Taleshaven.Infrastructure.Text;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure;

public static class DependencyInjection
{
    /// <param name="mediaPath">Mapp där uppladdade bilder lagras (utanför wwwroot).</param>
    public static IServiceCollection AddTaleshavenInfrastructure(this IServiceCollection services, string connectionString, string mediaPath)
    {
        // Fabriken används av tjänsterna (kortlivade contexts, säkert i Blazor Server).
        // AddDbContextFactory registrerar även TaleshavenDbContext som scoped, vilket Identity behöver.
        services.AddDbContextFactory<TaleshavenDbContext>(options => options.UseNpgsql(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignApplicationService, CampaignApplicationService>();
        services.AddScoped<IThreadService, ThreadService>();
        services.AddScoped<IChronicleService, ChronicleService>();
        services.AddScoped<ICharacterService, CharacterService>();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();
        services.AddSingleton<IDiceRoller, CryptoDiceRoller>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddSingleton<IImageStore>(new LocalImageStore(mediaPath));

        return services;
    }

    public static async Task MigrateTaleshavenDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaleshavenDbContext>();
        await db.Database.MigrateAsync();
    }
}
