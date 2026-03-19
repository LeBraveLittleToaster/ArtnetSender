namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that checklist items were scanned/inspected.</summary>
public sealed class ScanChecklistAction : RentalAction
{
    public long ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}
