using LumenForgeServer.Inventory.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>
/// Query parameters for the stock-binding conflict check endpoint.
/// </summary>
public sealed record RentalConflictQueryDto
{
    [Required]
    [JsonPropertyName("device_guid")]
    public Guid DeviceGuid { get; init; }

    [Required]
    [JsonPropertyName("start")]
    public string Start { get; init; } = string.Empty;

    [Required]
    [JsonPropertyName("end")]
    public string End { get; init; } = string.Empty;

    /// <summary>
    /// Binding type to check conflicts against. Defaults to <see cref="BindingType.RENTAL"/>.
    /// </summary>
    [JsonPropertyName("binding_type")]
    public BindingType BindingType { get; init; } = BindingType.RENTAL;

    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;
}
