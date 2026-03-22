using System.Text.Json.Serialization;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API representation of a single damage entry.</summary>
public sealed record DamageEntryDto
{
    [JsonPropertyName("stock_binding_guid")]
    public required Guid StockBindingGuid { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("severity")]
    public required DamageSeverity Severity { get; init; }
}

/// <summary>API request DTO for recording damages found during inspection.</summary>
public sealed record RecordDamagesDto : IActionInputDerivable<RecordDamagesInput>
{
    [JsonPropertyName("damages")]
    public required List<DamageEntryDto> Damages { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RecordDamagesInput ToActionInput() => new()
    {
        Damages = Damages.Select(d => new DamageEntry
        {
            StockBindingGuid = d.StockBindingGuid,
            Description = d.Description,
            Severity = d.Severity
        }).ToList()
    };
}

/// <summary>API request DTO for creating maintenance jobs for damaged items.</summary>
public sealed record CreateMaintenanceJobsDto : IActionInputDerivable<CreateMaintenanceJobsInput>
{
    [JsonPropertyName("damaged_stock_binding_guids")]
    public required List<Guid> DamagedStockBindingGuids { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public CreateMaintenanceJobsInput ToActionInput() => new()
    {
        DamagedStockBindingGuids = DamagedStockBindingGuids
    };
}
