using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Text;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Campaigns;
using Taleshaven.Infrastructure.Data;
using Taleshaven.Infrastructure.Text;
using Taleshaven.Infrastructure.Threads;

namespace Taleshaven.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaleshavenInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Fabriken används av tjänsterna (kortlivade contexts, säkert i Blazor Server).
        // AddDbContextFactory registrerar även TaleshavenDbContext som scoped, vilket Identity behöver.
        services.AddDbContextFactory<TaleshavenDbContext>(options => options.UseNpgsql(connectionString));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignApplicationService, CampaignApplicationService>();
        services.AddScoped<IThreadService, ThreadService>();
        services.AddSingleton<IMarkdownRenderer, MarkdownRenderer>();

        return services;
    }

    public static async Task MigrateTaleshavenDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TaleshavenDbContext>();
        await db.Database.MigrateAsync();
    }
}
