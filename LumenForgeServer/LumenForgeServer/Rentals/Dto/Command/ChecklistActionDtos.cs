using System.Text.Json.Serialization;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for generating a checklist.</summary>
public sealed record GenerateChecklistDto
{
    /// <summary>Type of checklist to generate (PICKUP or DROPOFF).</summary>
    [JsonPropertyName("checklist_type")]
    public required ChecklistType ChecklistType { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public GenerateChecklistInput ToActionInput() => new() { ChecklistType = ChecklistType };
}

/// <summary>API request DTO for recording a device scan against a checklist.</summary>
public sealed record ScanChecklistDto
{
    /// <summary>GUID of the checklist to scan against.</summary>
    [JsonPropertyName("checklist_guid")]
    public required Guid ChecklistGuid { get; init; }

    /// <summary>Value captured from the QR / barcode scan.</summary>
    [JsonPropertyName("scanned_value")]
    public required string ScannedValue { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public ScanChecklistInput ToActionInput() => new()
    {
        ChecklistGuid = ChecklistGuid,
        ScannedValue = ScannedValue
    };
}

/// <summary>API request DTO for signing a checklist.</summary>
public sealed record SignChecklistDto
{
    /// <summary>GUID of the checklist to sign.</summary>
    [JsonPropertyName("checklist_guid")]
    public required Guid ChecklistGuid { get; init; }

    /// <summary>Base-64 encoded signature image data.</summary>
    [JsonPropertyName("signature_data")]
    public required string SignatureData { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public SignChecklistInput ToActionInput() => new()
    {
        ChecklistGuid = ChecklistGuid,
        SignatureData = SignatureData
    };
}
