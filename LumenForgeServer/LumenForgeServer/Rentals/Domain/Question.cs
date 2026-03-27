namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// A survey question used for rental feedback collection.
/// </summary>
public class Question
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>The question text displayed to the user.</summary>
    public required string QuestionText { get; set; }

    /// <summary>Logical category for grouping questions.</summary>
    public string? Category { get; set; }

    /// <summary>Display order within the category.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether the question is currently active.</summary>
    public bool IsActive { get; set; } = true;

    public required QuestionDataType QuestionDataType { get; set; } = QuestionDataType.FREETEXT;

    /// <summary>Answers submitted for this question across rentals.</summary>
    public List<Answer> Answers { get; set; } = [];
}
