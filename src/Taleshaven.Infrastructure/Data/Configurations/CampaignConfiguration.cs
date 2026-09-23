using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Campaigns;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(CampaignLimits.NameMaxLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(CampaignLimits.DescriptionMaxLength);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.GameMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Memberships)
            .WithOne()
            .HasForeignKey(m => m.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Applications)
            .WithOne()
            .HasForeignKey(a => a.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Status);
    }
}

internal sealed class CampaignApplicationConfiguration : IEntityTypeConfiguration<CampaignApplication>
{
    public void Configure(EntityTypeBuilder<CampaignApplication> builder)
    {
        // Id skapas i domänen (Guid v7), så att nya ansökningar alltid sparas som nya rader.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(CampaignLimits.ApplicationMessageMaxLength);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.DecidedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Högst en väntande ansökan per användare och kampanj, även vid samtidiga anrop.
        builder.HasIndex(a => new { a.CampaignId, a.UserId })
            .IsUnique()
            .HasFilter($"\"{nameof(CampaignApplication.Status)}\" = {(int)ApplicationStatus.Pending}");
    }
}

internal sealed class CampaignMembershipConfiguration : IEntityTypeConfiguration<CampaignMembership>
{
    public void Configure(EntityTypeBuilder<CampaignMembership> builder)
    {
        builder.HasKey(m => new { m.CampaignId, m.UserId });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
