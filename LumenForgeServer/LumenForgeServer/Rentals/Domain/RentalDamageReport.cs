using LumenForgeServer.Common;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Records damage found on a specific item during post-return inspection.
/// Created by <see cref="Actions.Handlers.RecordDamagesHandler"/>.
/// </summary>
public class RentalDamageReport
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the owning process instance.</summary>
    public long ProcessInstanceId { get; set; }

    /// <summary>Navigation to the owning process instance.</summary>
    public RentalProcessInstance ProcessInstance { get; set; } = null!;

    /// <summary>GUID of the stock binding (item) that is damaged.</summary>
    public Guid StockBindingGuid { get; set; }

    /// <summary>Free-text description of the damage.</summary>
    public required string Description { get; set; }

    /// <summary>Severity classification.</summary>
    public DamageSeverity Severity { get; set; }

    /// <summary>Keycloak id of the user who recorded the damage.</summary>
    public required string ReportedByKcId { get; set; }

    /// <summary>Instant the damage was recorded.</summary>
    public Instant ReportedAt { get; set; }
}
