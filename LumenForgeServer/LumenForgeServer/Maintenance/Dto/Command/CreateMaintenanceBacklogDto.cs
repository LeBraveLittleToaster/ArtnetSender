using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a maintenance backlog entry.
/// At least one of <see cref="DeviceUuid"/>, <see cref="RentalItemUuid"/> must be provided.
/// </summary>
public sealed record CreateMaintenanceBacklogDto
{
    /// <summary>
    /// UUID of the maintenance backlog status to assign.
    /// </summary>
    [Required]
    [JsonPropertyName("status_uuid")]
    public required Guid StatusUuid { get; init; }

    /// <summary>
    /// Short summary of the issue.
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("issue_summary")]
    public required string IssueSummary { get; init; }

    /// <summary>
    /// Optional detailed description of the issue.
    /// </summary>
    [StringLength(4000)]
    [JsonPropertyName("issue_description")]
    public string? IssueDescription { get; init; }

    /// <summary>
    /// Quantity of units affected. Must be greater than zero.
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity affected must be greater than zero.")]
    [JsonPropertyName("quantity_affected")]
    public decimal QuantityAffected { get; init; }

    /// <summary>
    /// Optional UUID of a directly affected device (for device-level maintenance).
    /// </summary>
    [JsonPropertyName("device_uuid")]
    public Guid? DeviceUuid { get; init; }

    /// <summary>
    /// Optional UUID of the rental item that triggered this backlog entry.
    /// </summary>
    [JsonPropertyName("rental_item_uuid")]
    public Guid? RentalItemUuid { get; init; }
}
