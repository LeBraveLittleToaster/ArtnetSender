using System.Text.Json.Serialization;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for generating an invoice.</summary>
public sealed record GenerateInvoiceDto
{
    [JsonPropertyName("due_date_override")]
    public string? DueDateOverride { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public GenerateInvoiceInput ToActionInput() => new() { DueDateOverride = DueDateOverride };
}

/// <summary>API request DTO for recording a payment against an invoice.</summary>
public sealed record RecordPaymentDto
{
    [JsonPropertyName("invoice_guid")]
    public required Guid InvoiceGuid { get; init; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; init; }

    [JsonPropertyName("method")]
    public required PaymentMethod Method { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RecordPaymentInput ToActionInput() => new()
    {
        InvoiceGuid = InvoiceGuid,
        Amount = Amount,
        Method = Method,
        Reference = Reference
    };
}
