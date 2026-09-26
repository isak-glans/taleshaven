using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Infrastructure.Data.Configurations;

/// <summary>Sajtens roller (B18) ligger i Identitys rolltabell med fasta id:n, så att de finns från första migreringen.</summary>
internal sealed class SiteRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(SiteRoleIds.All.Select(pair => new IdentityRole
        {
            Id = pair.Value,
            Name = pair.Key.ToString(),
            NormalizedName = pair.Key.ToString().ToUpperInvariant(),
            ConcurrencyStamp = pair.Value,
        }));
    }
}
