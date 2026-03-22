using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for approving a rental request.</summary>
public sealed record ApproveRequestDto : IActionInputDerivable<ApproveRequestInput>
{
    /// <summary>Optional comment for the approval (visible in the audit log).</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public ApproveRequestInput ToActionInput() => new() { Comment = Comment };
}

/// <summary>API request DTO for rejecting a rental request.</summary>
public sealed record RejectRequestDto : IActionInputDerivable<RejectRequestInput>
{
    /// <summary>Reason for rejecting the request (required).</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RejectRequestInput ToActionInput() => new() { Reason = Reason };
}
