using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for partially updating a maintenance backlog entry.
/// </summary>
public sealed record UpdateMaintenanceBacklogDto
{
    /// <summary>
    /// New status UUID to assign, if changing.
    /// </summary>
    [JsonPropertyName("status_uuid")]
    public Guid? StatusUuid { get; init; }

    /// <summary>
    /// Updated short summary of the issue.
    /// </summary>
    [StringLength(2000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("issue_summary")]
    public string? IssueSummary { get; init; }

    /// <summary>
    /// Updated detailed description, or null to clear it.
    /// </summary>
    [StringLength(4000)]
    [JsonPropertyName("issue_description")]
    public string? IssueDescription { get; init; }

    /// <summary>
    /// Updated quantity affected.
    /// </summary>
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity affected must be greater than zero.")]
    [JsonPropertyName("quantity_affected")]
    public decimal? QuantityAffected { get; init; }

    /// <summary>
    /// When set to true, marks the issue as resolved with the current timestamp.
    /// </summary>
    [JsonPropertyName("resolve")]
    public bool? Resolve { get; init; }
}
