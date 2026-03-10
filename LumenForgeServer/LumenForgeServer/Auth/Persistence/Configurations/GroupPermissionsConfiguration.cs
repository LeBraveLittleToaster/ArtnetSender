using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="GroupPermissions"/>.
/// </summary>
public sealed class GroupPermissionsConfiguration : IEntityTypeConfiguration<GroupPermissions>
{
    public void Configure(EntityTypeBuilder<GroupPermissions> builder)
    {
        builder.ToTable("group_roles");

        builder.HasKey(x => new { x.GroupId, RoleId = x.Permission });
        builder.Property(x => x.Permission).HasConversion<int>();
    }
}
