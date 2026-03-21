using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Generic audit record written by the <see cref="RentalActionService"/> after every
/// action execution, regardless of whether the action succeeded or failed.
/// Enables full traceability of the rental process without coupling to any
/// specific action handler.
/// </summary>
public class RentalActionLog
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier for external references.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the owning <see cref="RentalProcessInstance"/>.</summary>
    public long ProcessInstanceId { get; set; }

    /// <summary>Navigation to the owning process instance.</summary>
    public RentalProcessInstance ProcessInstance { get; set; } = null!;

    /// <summary>The action that was executed.</summary>
    public RentalActionType ActionType { get; set; }

    /// <summary>Keycloak subject id of the actor who triggered the action.</summary>
    public required string PerformedByKcId { get; set; }

    /// <summary>Stage the process was in before the action ran.</summary>
    public RentalStage StageBefore { get; set; }

    /// <summary>Stage the process transitioned to after the action ran.</summary>
    public RentalStage StageAfter { get; set; }

    /// <summary>Whether the action completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Human-readable error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Optional JSON snapshot of the action input for reproducibility.</summary>
    public string? PayloadJson { get; set; }

    /// <summary>Instant the action was executed.</summary>
    public Instant PerformedAt { get; set; }
}
