using LumenForgeServer.Common;

namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a checklist was generated (PICKUP or DROPOFF).</summary>
public sealed class GenerateChecklistAction : RentalAction
{
    public ChecklistType ChecklistType { get; set; }

    public long ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}
