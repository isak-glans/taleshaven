using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Portraits;
using Taleshaven.Core.Users;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(UserLimits.DisplayNameMaxLength);

        builder.Property(u => u.About)
            .IsRequired()
            .HasMaxLength(UserLimits.AboutMaxLength)
            .HasDefaultValue("");

        // Tas porträttet bort ur biblioteket får användaren initialer igen (B50, som B20 för karaktärer).
        builder.HasOne<Portrait>()
            .WithMany()
            .HasForeignKey(u => u.PortraitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
