using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Portraits;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class PortraitConfiguration : IEntityTypeConfiguration<Portrait>
{
    public void Configure(EntityTypeBuilder<Portrait> builder)
    {
        builder.Property(p => p.ImageKey).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Source).HasMaxLength(PortraitTags.SourceMaxLength);

        // Taggarna lagras som en text[]-kolumn; GIN-indexet gör sökning på taggar snabb (PB-4).
        builder.Property(p => p.Tags).IsRequired();
        builder.HasIndex(p => p.Tags).HasMethod("gin");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
