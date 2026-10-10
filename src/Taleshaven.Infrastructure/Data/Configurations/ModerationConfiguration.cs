using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Moderation;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

// Rapporter, moderationslogg och meddelanden till användare (B70).

internal sealed class PostReportConfiguration : IEntityTypeConfiguration<PostReport>
{
    public void Configure(EntityTypeBuilder<PostReport> builder)
    {
        builder.Property(r => r.Comment).HasMaxLength(ModerationLimits.CommentMaxLength);

        // En användare rapporterar samma inlägg högst en gång.
        builder.HasIndex(r => new { r.PostId, r.ReporterId }).IsUnique();
        builder.HasIndex(r => r.Status);

        // Rapporterna följer med när inlägget försvinner (en raderad kampanj tar med sig sina inlägg).
        builder.HasOne<Post>().WithMany().HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.Property(a => a.Reason).HasMaxLength(ModerationLimits.ReasonMaxLength);
        builder.HasIndex(a => a.CampaignId);
        builder.HasIndex(a => a.CreatedAt);

        // Loggen är historik och pekar inte med främmande nycklar; ett borttaget inlägg eller konto ska inte radera raden.
    }
}

internal sealed class UserNoticeConfiguration : IEntityTypeConfiguration<UserNotice>
{
    public void Configure(EntityTypeBuilder<UserNotice> builder)
    {
        builder.Property(n => n.Message).HasMaxLength(ModerationLimits.ReasonMaxLength);
        builder.HasIndex(n => n.UserId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
