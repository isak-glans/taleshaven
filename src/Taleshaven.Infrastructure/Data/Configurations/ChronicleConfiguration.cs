using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Chronicle;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class ChronicleChapterConfiguration : IEntityTypeConfiguration<ChronicleChapter>
{
    public void Configure(EntityTypeBuilder<ChronicleChapter> builder)
    {
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(ChronicleLimits.TitleMaxLength);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(ChronicleLimits.ContentMaxLength);

        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Inget unikt index på platsen: två kapitel byter plats i samma sparning när GM flyttar dem.
        builder.HasIndex(c => new { c.CampaignId, c.Position });
    }
}
