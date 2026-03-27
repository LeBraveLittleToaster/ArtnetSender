using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API representation of a single item assignment.</summary>
public sealed record ItemAssignmentDto
{
    /// <summary>GUID of the inventory device to assign.</summary>
    [JsonPropertyName("device_guid")]
    public required Guid DeviceGuid { get; init; }

    /// <summary>Number of units to reserve.</summary>
    [JsonPropertyName("quantity")]
    public required long Quantity { get; init; }
}

/// <summary>API request DTO for assigning inventory items to a rental.</summary>
public sealed record AssignItemsDto : IActionInputDerivable<AssignItemsInput>
{
    /// <summary>List of device/quantity pairs to assign.</summary>
    [JsonPropertyName("items")]
    public required List<ItemAssignmentDto> Items { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public AssignItemsInput ToActionInput() => new()
    {
        Items = Items.Select(i => new ItemAssignment
        {
            DeviceGuid = i.DeviceGuid,
            Quantity = i.Quantity
        }).ToList()
    };
}

/// <summary>API request DTO for removing assigned items from a rental.</summary>
public sealed record RemoveItemsDto : IActionInputDerivable<RemoveItemsInput>
{
    /// <summary>Stock-binding GUIDs of the items to remove.</summary>
    [JsonPropertyName("stock_binding_guids")]
    public required List<Guid> StockBindingGuids { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RemoveItemsInput ToActionInput() => new() { StockBindingGuids = StockBindingGuids };
}

/// <summary>API request DTO for approving the assigned item list.</summary>
public sealed record ApproveItemsDto : IActionInputDerivable<ApproveItemsInput>
{
    /// <summary>Optional approval comment.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public ApproveItemsInput ToActionInput() => new() { Comment = Comment };
}

/// <summary>API request DTO for rejecting the assigned item list.</summary>
public sealed record RejectItemsDto : IActionInputDerivable<RejectItemsInput>
{
    /// <summary>Reason for rejecting the item list (required).</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RejectItemsInput ToActionInput() => new() { Reason = Reason };
}
