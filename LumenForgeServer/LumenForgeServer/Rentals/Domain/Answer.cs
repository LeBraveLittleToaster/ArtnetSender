namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// A user-submitted answer to a <see cref="Question"/>.
/// </summary>
public class Answer
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the rental this answer belongs to.</summary>
    public long RentalId { get; set; }

    /// <summary>Navigation to the parent rental.</summary>
    public Rental Rental { get; set; } = null!;

    /// <summary>Foreign key to the question being answered.</summary>
    public long QuestionId { get; set; }

    /// <summary>Navigation to the parent question.</summary>
    public Question Question { get; set; } = null!;

    /// <summary>The answer value provided by the user.</summary>
    public required string Value { get; set; }
}
