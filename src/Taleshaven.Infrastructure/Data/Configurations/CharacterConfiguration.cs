using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Portraits;
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
        builder.Property(c => c.Alias).HasMaxLength(CharacterLimits.NameMaxLength);

        // Räknare, tillstånd och sparade slag (B56) lagras som jsonb på karaktären, som tärningsslagen på inlägg.
        builder.OwnsMany(c => c.Counters, counter => counter.ToJson());
        builder.OwnsMany(c => c.Conditions, condition => condition.ToJson());
        builder.OwnsMany(c => c.SavedRolls, roll => roll.ToJson());

        // Tas porträttet bort ur biblioteket får karaktären initialer (B20).
        builder.HasOne<Portrait>()
            .WithMany()
            .HasForeignKey(c => c.PortraitId)
            .OnDelete(DeleteBehavior.SetNull);

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
