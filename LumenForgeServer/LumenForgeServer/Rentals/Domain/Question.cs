using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a predefined survey question for rental feedback/experience.
/// Questions are immutable and managed by the system.
/// </summary>
public class Question
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    /// <summary>
    /// The question text displayed to users.
    /// </summary>
    public string QuestionText { get; set; } = null!;

    /// <summary>
    /// Optional category grouping (e.g., "Experience", "Equipment", "Service").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Display order within the survey.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this question is currently active and should be shown to users.
    /// </summary>
    public bool IsActive { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }

    public List<Answer> Answers { get; set; } = [];
}
