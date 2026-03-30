using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Question"/>.
/// </summary>
public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("question");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.QuestionText).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(256);

    }
}
