using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Group"/>.
/// </summary>
public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.GroupRoles)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.GroupUsers)
            .WithOne(x => x.Group)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
