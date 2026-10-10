using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Messages;
using Taleshaven.Core.Threads;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // Konversationens meddelanden ligger i en egen tråd (B73); tas tråden bort försvinner konversationen.
        builder.HasOne<CampaignThread>()
            .WithMany()
            .HasForeignKey(c => c.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.ThreadId).IsUnique();

        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserAId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.UserBId).OnDelete(DeleteBehavior.Restrict);

        // Högst en konversation per par; paret sparas i ordning (A < B, jämfört bytevis som string.CompareOrdinal).
        builder.HasIndex(c => new { c.UserAId, c.UserBId }).IsUnique();
        builder.HasIndex(c => c.UserBId);
        builder.ToTable(t => t.HasCheckConstraint("CK_Conversations_Pair", "\"UserAId\" < \"UserBId\" COLLATE \"C\""));
    }
}

internal sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.HasKey(b => new { b.BlockerId, b.BlockedId });
        builder.HasIndex(b => b.BlockedId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.BlockerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(b => b.BlockedId).OnDelete(DeleteBehavior.Cascade);
    }
}
