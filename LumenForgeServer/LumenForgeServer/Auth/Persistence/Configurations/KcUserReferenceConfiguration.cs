using LumenForgeServer.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Auth.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="KcUserReference"/>.
/// </summary>
public sealed class KcUserReferenceConfiguration : IEntityTypeConfiguration<KcUserReference>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
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
