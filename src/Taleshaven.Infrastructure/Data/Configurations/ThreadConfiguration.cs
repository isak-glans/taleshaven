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

        // Hette Description före fas 6.
        builder.Property(t => t.Introduction)
            .IsRequired()
            .HasMaxLength(ThreadLimits.IntroductionMaxLength);

        builder.Property(t => t.Chronicle)
            .HasMaxLength(ThreadLimits.ChronicleMaxLength);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.ChronicleEditedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Trådlistan sorteras på position (B27). Det finns inga fasta trådar längre, så ingen unik OOC-tråd (B25).
        builder.HasIndex(t => new { t.CampaignId, t.Position });
    }
}

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(ThreadLimits.PostStorageMaxLength);

        // En karaktär som har skrivit inlägg kan inte tas bort, så att gamla inlägg behåller sin karaktär.
        // NO ACTION (inte RESTRICT) kontrolleras först när hela satsen är klar, så att en raderad kampanj
        // kan ta med sig både inlägg och karaktärer i samma kaskad.
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Tärningsslagen i texten (B31) lagras som jsonb i samma rad som inlägget.
        builder.OwnsMany(p => p.Rolls, roll => roll.ToJson());

        // NO ACTION av samma skäl som för karaktären: en raderad kampanj tar med sig både svaret och originalet.
        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(p => p.ReplyToPostId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);

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

internal sealed class ReadMarkerConfiguration : IEntityTypeConfiguration<ReadMarker>
{
    public void Configure(EntityTypeBuilder<ReadMarker> builder)
    {
        builder.HasKey(m => new { m.UserId, m.ThreadId });

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CampaignThread>()
            .WithMany()
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PostRevisionConfiguration : IEntityTypeConfiguration<PostRevision>
{
    public void Configure(EntityTypeBuilder<PostRevision> builder)
    {
        builder.Property(r => r.Content)
            .IsRequired()
            .HasMaxLength(ThreadLimits.PostStorageMaxLength);

        builder.HasOne<Post>()
            .WithMany()
            .HasForeignKey(r => r.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
