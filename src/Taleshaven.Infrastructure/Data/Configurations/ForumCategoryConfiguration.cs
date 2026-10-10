using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Core.Forum;

namespace Taleshaven.Infrastructure.Data.Configurations;

internal sealed class ForumCategoryConfiguration : IEntityTypeConfiguration<ForumCategory>
{
    public void Configure(EntityTypeBuilder<ForumCategory> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(60);
        builder.Property(c => c.Description).IsRequired().HasMaxLength(300);
    }
}
