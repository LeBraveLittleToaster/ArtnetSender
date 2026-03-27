using LumenForgeServer.Inventory.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Create;

public sealed record CreateDeviceRelationDto
{
    [JsonPropertyName("parent_device_guid")]
    public required Guid ParentDeviceGuid { get; init; }

    [JsonPropertyName("child_device_guid")]
    public required Guid ChildDeviceGuid { get; init; }

    [Range(1, long.MaxValue)]
    [JsonPropertyName("contained_amount")]
    public long ContainedAmount { get; init; } = 1;

    [JsonPropertyName("relation_type")]
    public DeviceRelationType RelationType { get; init; } = DeviceRelationType.Flexible;
}
