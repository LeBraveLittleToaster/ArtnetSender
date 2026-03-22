using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for generating a summary report.</summary>
public sealed record GenerateReportDto : IActionInputDerivable<GenerateReportInput>
{
    [JsonPropertyName("include_damages")]
    public bool IncludeDamages { get; init; } = true;

    [JsonPropertyName("include_payments")]
    public bool IncludePayments { get; init; } = true;

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public GenerateReportInput ToActionInput() => new()
    {
        IncludeDamages = IncludeDamages,
        IncludePayments = IncludePayments
    };
}
