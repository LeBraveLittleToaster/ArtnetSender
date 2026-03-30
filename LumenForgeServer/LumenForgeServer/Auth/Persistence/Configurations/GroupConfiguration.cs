using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Group"/>.
/// </summary>
public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
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
