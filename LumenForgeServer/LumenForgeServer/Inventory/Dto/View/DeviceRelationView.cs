using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

public sealed record DeviceRelationView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("parent_device_guid")]
    public Guid ParentDeviceGuid { get; init; }

    [JsonPropertyName("child_device_guid")]
    public Guid ChildDeviceGuid { get; init; }

    [JsonPropertyName("parent_device_name")]
    public string? ParentDeviceName { get; init; }

    [JsonPropertyName("child_device_name")]
    public string? ChildDeviceName { get; init; }

    [JsonPropertyName("contained_amount")]
    public long ContainedAmount { get; init; }

    [JsonPropertyName("relation_type")]
    public DeviceRelationType RelationType { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="relation">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static DeviceRelationView FromEntity(DeviceRelation relation)
    {
        return new DeviceRelationView
        {
            Guid = relation.Guid,
            ParentDeviceGuid = relation.ParentDevice.Guid,
            ChildDeviceGuid = relation.ChildDevice.Guid,
            ParentDeviceName = relation.ParentDevice.DeviceName,
            ChildDeviceName = relation.ChildDevice.DeviceName,
            ContainedAmount = relation.ContainedAmount,
            RelationType = relation.RelationType,
            CreatedAt = relation.CreatedAt,
            UpdatedAt = relation.UpdatedAt
        };
    }
}
