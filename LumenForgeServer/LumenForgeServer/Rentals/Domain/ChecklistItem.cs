using LumenForgeServer.Maintenance.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a checklist verification row tied to a rental item.
/// </summary>
public class ChecklistItem
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public long RentalItemId { get; set; }
    public RentalItem RentalItem { get; set; } = null!;

    public decimal QuantityChecked { get; set; } // > 0
    public bool ConditionOk { get; set; }
    public string? ConditionNotes { get; set; }

    public decimal DamagedQuantity { get; set; } // >= 0
    public string? DamageSummary { get; set; }
    public string? DamageDescription { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
