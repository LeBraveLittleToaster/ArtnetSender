using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="GroupUser"/>.
/// </summary>
public sealed class GroupUserConfiguration : IEntityTypeConfiguration<GroupUser>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<GroupUser> builder)
    {
        builder.ToTable("group_users");

        builder.HasKey(x => new { x.GroupId, x.UserId });
        builder.HasIndex(x => x.UserId);
    }
}
