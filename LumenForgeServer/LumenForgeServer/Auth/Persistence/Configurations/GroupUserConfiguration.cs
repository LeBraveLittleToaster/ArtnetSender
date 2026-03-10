using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="GroupUser"/>.
/// </summary>
public sealed class GroupUserConfiguration : IEntityTypeConfiguration<GroupUser>
{
    public void Configure(EntityTypeBuilder<GroupUser> builder)
    {
        builder.ToTable("group_users");

        builder.HasKey(x => new { x.GroupId, x.UserId });
        builder.HasIndex(x => x.UserId);
    }
}
