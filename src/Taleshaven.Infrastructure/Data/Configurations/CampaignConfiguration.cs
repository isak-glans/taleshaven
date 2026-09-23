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

        builder.HasIndex(c => c.Status);
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
