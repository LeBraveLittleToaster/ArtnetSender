using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for creating a new rental.</summary>
public sealed record CreateRentalDto : IActionInputDerivable<CreateRentalInput>
{
    /// <summary>Full name of the customer requesting the rental.</summary>
    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; init; }

    /// <summary>Contact e-mail for the customer.</summary>
    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; init; }

    /// <summary>Optional owning group GUID for group-owned rentals.</summary>
    [JsonPropertyName("group_guid")]
    public Guid? GroupGuid { get; init; }

    /// <summary>Reason or purpose for the rental.</summary>
    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    /// <summary>Desired start of the rental period (NodaTime Instant).</summary>
    [JsonPropertyName("requested_start")]
    public required Instant RequestedStart { get; init; }

    /// <summary>Desired end of the rental period (NodaTime Instant). Must be after <c>requested_start</c>.</summary>
    [JsonPropertyName("requested_end")]
    public required Instant RequestedEnd { get; init; }

    /// <summary>Optional free-text notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>
    /// Question Guid with the provided answers
    /// </summary>
    [JsonPropertyName("answers")]
    public List<QASet> QASets { get; init; } = [];

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public CreateRentalInput ToActionInput() => new()
    {
        CustomerName = CustomerName,
        CustomerEmail = CustomerEmail,
        GroupGuid = GroupGuid,
        Purpose = Purpose,
        RequestedStart = RequestedStart,
        RequestedEnd = RequestedEnd,
        Notes = Notes,
        QASets = QASets
    };
}

public class QASet
{
    /// <summary>
    /// Question GUID
    /// </summary>
    [JsonPropertyName("question_guid")] 
    public required string Guid { get; init; }
    /// <summary>
    /// Serialized answer with unspecified datatype
    /// </summary>
    [JsonPropertyName("answer")] 
    public required string Value { get; init; }
}
