using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="KcUserReference"/>.
/// </summary>
public sealed class KcUserReferenceConfiguration : IEntityTypeConfiguration<KcUserReference>
{
    public void Configure(EntityTypeBuilder<KcUserReference> builder)
    {
        builder.ToTable("users_kc_reference");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.UserKcId).IsUnique();

        builder.HasMany(x => x.GroupUsers)
            .WithOne(x => x.KcUserReference)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
