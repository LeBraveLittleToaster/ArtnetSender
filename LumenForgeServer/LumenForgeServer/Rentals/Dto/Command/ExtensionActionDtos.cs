using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for submitting an extension request.</summary>
public sealed record RequestExtensionDto
{
    /// <summary>New desired end date for the rental period (NodaTime Instant).</summary>
    [JsonPropertyName("new_requested_end")]
    public required Instant NewRequestedEnd { get; init; }

    /// <summary>Optional reason for requesting the extension.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RequestExtensionInput ToActionInput() => new()
    {
        NewRequestedEnd = NewRequestedEnd,
        Reason = Reason
    };
}

/// <summary>API request DTO for approving an extension request.</summary>
public sealed record ApproveExtensionDto
{
    /// <summary>GUID of the extension to approve.</summary>
    [JsonPropertyName("extension_guid")]
    public required Guid ExtensionGuid { get; init; }

    /// <summary>Optional approval comment.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public ApproveExtensionInput ToActionInput() => new()
    {
        ExtensionGuid = ExtensionGuid,
        Comment = Comment
    };
}

/// <summary>API request DTO for rejecting an extension request.</summary>
public sealed record RejectExtensionDto
{
    /// <summary>GUID of the extension to reject.</summary>
    [JsonPropertyName("extension_guid")]
    public required Guid ExtensionGuid { get; init; }

    /// <summary>Reason for rejecting the extension (required).</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RejectExtensionInput ToActionInput() => new()
    {
        ExtensionGuid = ExtensionGuid,
        Reason = Reason
    };
}
