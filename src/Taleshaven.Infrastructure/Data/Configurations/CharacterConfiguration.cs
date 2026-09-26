using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(CharacterLimits.NameMaxLength);
        builder.Property(c => c.Sheet).IsRequired().HasMaxLength(CharacterLimits.SheetMaxLength);
        builder.Property(c => c.SheetUrl).HasMaxLength(CharacterLimits.SheetUrlMaxLength);
        builder.Property(c => c.RuleSystem).HasMaxLength(CharacterLimits.RuleSystemMaxLength);
        builder.Property(c => c.GmNote).HasMaxLength(CharacterLimits.GmNoteMaxLength);
        builder.Property(c => c.AvatarKey).HasMaxLength(64);

        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.CampaignId, c.OwnerId });
    }
}
