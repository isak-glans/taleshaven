using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data;

public class TaleshavenDbContext(DbContextOptions<TaleshavenDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMembership> CampaignMemberships => Set<CampaignMembership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TaleshavenDbContext).Assembly);
    }
}
