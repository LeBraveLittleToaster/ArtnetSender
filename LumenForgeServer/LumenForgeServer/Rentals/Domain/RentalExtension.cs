using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a request to extend the rental period.
/// Created by <see cref="RequestExtensionHandler"/> and
/// resolved by <see cref="ApproveExtensionHandler"/> or
/// <see cref="RejectExtensionHandler"/>.
/// </summary>
public class RentalExtension
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the owning process instance.</summary>
    public long ProcessInstanceId { get; set; }

    /// <summary>Navigation to the owning process instance.</summary>
    public RentalProcessInstance ProcessInstance { get; set; } = null!;

    /// <summary>New requested end date for the rental period.</summary>
    public Instant NewRequestedEnd { get; set; }

    /// <summary>Original end date at the time the extension was requested.</summary>
    public Instant OriginalEnd { get; set; }

    /// <summary>Reason the extension is needed.</summary>
    public string? Reason { get; set; }

    /// <summary>Whether the extension has been approved.</summary>
    public bool? IsApproved { get; set; }

    /// <summary>Comment from the reviewer.</summary>
    public string? ReviewComment { get; set; }

    /// <summary>Keycloak id of the user who requested the extension.</summary>
    public required string RequestedByKcId { get; set; }

    /// <summary>Keycloak id of the reviewer.</summary>
    public string? ReviewedByKcId { get; set; }

    /// <summary>Instant the extension was requested.</summary>
    public Instant RequestedAt { get; set; }

    /// <summary>Instant the extension was reviewed.</summary>
    public Instant? ReviewedAt { get; set; }
}
