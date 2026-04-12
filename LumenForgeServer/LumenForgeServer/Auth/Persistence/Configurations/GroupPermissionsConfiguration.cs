using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="GroupPermissions"/>.
/// </summary>
public sealed class GroupPermissionsConfiguration : IEntityTypeConfiguration<GroupPermissions>
{
    /// <summary>
    /// Evaluates authorization rules for the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<GroupPermissions> builder)
    {
        builder.ToTable("group_roles");

        builder.HasKey(x => new { x.GroupId, RoleId = x.Permission });
        builder.Property(x => x.Permission).HasConversion<int>();
    }
}
