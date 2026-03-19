namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a checklist was signed (finalized and made immutable).</summary>
public sealed class SignChecklistAction : RentalAction
{
    public long ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    public string? Notes { get; set; }
}
