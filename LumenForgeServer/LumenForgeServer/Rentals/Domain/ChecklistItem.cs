using LumenForgeServer.Maintenance.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a checklist verification row tied to a rental item.
/// <para>
/// <see cref="IsChecked"/> is <c>false</c> on generation and becomes <c>true</c> when staff
/// submit an inspection result, enabling partial-completion tracking before the parent
/// <see cref="Checklist"/> is signed.
/// </para>
/// </summary>
public class ChecklistItem
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public long RentalItemId { get; set; }
    public RentalItem RentalItem { get; set; } = null!;

    /// <summary>
    /// False when auto-generated (not yet inspected); true once staff submit an inspection result.
    /// </summary>
    public bool IsChecked { get; set; }

    public decimal QuantityChecked { get; set; } // >= 0
    public bool ConditionOk { get; set; }
    public string? ConditionNotes { get; set; }

    public decimal DamagedQuantity { get; set; } // >= 0
    public string? DamageSummary { get; set; }
    public string? DamageDescription { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
