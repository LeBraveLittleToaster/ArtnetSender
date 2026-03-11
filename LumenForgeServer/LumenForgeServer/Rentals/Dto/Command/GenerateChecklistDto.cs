using LumenForgeServer.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for generating a checklist for a rental.
/// For <see cref="ChecklistType.DROPOFF"/>, <see cref="SourceChecklistGuid"/> must reference
/// an existing PICKUP checklist on the same rental.
/// </summary>
public sealed record GenerateChecklistDto
{
    [Required]
    [JsonPropertyName("checklist_type")]
    public required ChecklistType ChecklistType { get; init; }

    /// <summary>
    /// Required when <see cref="ChecklistType"/> is <c>DROPOFF</c>.
    /// The PICKUP checklist whose items are mirrored into the new DROPOFF checklist.
    /// </summary>
    [JsonPropertyName("source_checklist_guid")]
    public Guid? SourceChecklistGuid { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}
