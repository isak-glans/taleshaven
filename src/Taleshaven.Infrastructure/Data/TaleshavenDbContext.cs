using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data;

public class TaleshavenDbContext(DbContextOptions<TaleshavenDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMembership> CampaignMemberships => Set<CampaignMembership>();
    public DbSet<CampaignApplication> CampaignApplications => Set<CampaignApplication>();
    public DbSet<CampaignThread> Threads => Set<CampaignThread>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TaleshavenDbContext).Assembly);
    }
}
