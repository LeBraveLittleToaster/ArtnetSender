using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// A single line item within a <see cref="Checklist"/>.
/// Represents one device that must be scanned during pickup or dropoff.
/// </summary>
public class ChecklistItem
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the parent checklist.</summary>
    public long ChecklistId { get; set; }

    /// <summary>Navigation to the parent checklist.</summary>
    public Checklist Checklist { get; set; } = null!;

    /// <summary>GUID of the stock binding this item represents.</summary>
    public Guid StockBindingGuid { get; set; }

    /// <summary>Display name of the device (denormalized for the checklist).</summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>Whether this item has been scanned.</summary>
    public bool IsScanned { get; set; }

    /// <summary>Value captured during the scan (serial number or barcode).</summary>
    public string? ScannedValue { get; set; }

    /// <summary>Keycloak id of the user who scanned this item.</summary>
    public string? ScannedByKcId { get; set; }

    /// <summary>Instant the item was scanned.</summary>
    public Instant? ScannedAt { get; set; }
}
