using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a user's answer to a rental feedback question.
/// Linked to both a Question and optionally a RentalEvent for audit/tracking.
/// </summary>
public class Answer
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    // Optional: link to a rental event for audit trail
    public long? RentalEventId { get; set; }
    public RentalEvent? RentalEvent { get; set; }

    public long? RentalId { get; set; }
    public Rental? Rental { get; set; }

    /// <summary>
    /// The answer response: "Yes", "No", "NotImportant", or "Unknown".
    /// </summary>
    public string Response { get; set; } = null!;

    /// <summary>
    /// Optional free-text explanation or comment.
    /// </summary>
    public string? Comment { get; set; }

    // Keycloak user id (nullable for anonymous feedback)
    public string? RespondentUserId { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
