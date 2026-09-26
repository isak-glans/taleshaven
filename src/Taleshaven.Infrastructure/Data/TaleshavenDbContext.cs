using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Chronicle;
using Taleshaven.Core.Portraits;
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
    public DbSet<PostRevision> PostRevisions => Set<PostRevision>();
    public DbSet<ReadMarker> ReadMarkers => Set<ReadMarker>();
    public DbSet<ChronicleChapter> ChronicleChapters => Set<ChronicleChapter>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Portrait> Portraits => Set<Portrait>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(TaleshavenDbContext).Assembly);
    }
}
