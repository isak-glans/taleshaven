using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Campaigns;
using Taleshaven.Core.Characters;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class CampaignThreadConfiguration : IEntityTypeConfiguration<CampaignThread>
{
    public void Configure(EntityTypeBuilder<CampaignThread> builder)
    {
        builder.ToTable("Threads");

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(ThreadLimits.TitleMaxLength);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(ThreadLimits.DescriptionMaxLength);

        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.CampaignId, t.Kind });

        // Exakt en OOC-tråd per kampanj.
        builder.HasIndex(t => t.CampaignId)
            .IsUnique()
            .HasFilter($"\"{nameof(CampaignThread.Kind)}\" = {(int)ThreadKind.Ooc}")
            .HasDatabaseName("IX_Threads_CampaignId_Ooc");
    }
}

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(ThreadLimits.PostMaxLength);

        // En karaktär som har skrivit inlägg kan inte tas bort, så att gamla inlägg behåller sin karaktär.
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tärningskastet lagras som jsonb i samma rad som inlägget (null för vanliga inlägg).
        builder.OwnsOne(p => p.Roll, roll => roll.ToJson());

        builder.HasOne<CampaignThread>()
            .WithMany()
            .HasForeignKey(p => p.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stödjer sidhämtning (keyset) per tråd.
        builder.HasIndex(p => new { p.ThreadId, p.Id });
    }
}

internal sealed class PostRevisionConfiguration : IEntityTypeConfiguration<PostRevision>
{
    public void Configure(EntityTypeBuilder<PostRevision> builder)
    {
        builder.Property(r => r.Content)
            .IsRequired()
            .HasMaxLength(ThreadLimits.PostMaxLength);

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
