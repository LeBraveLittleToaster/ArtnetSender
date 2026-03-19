namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records cancellation of a rental. Status → Cancelled.</summary>
public sealed class CancelRentalAction : RentalAction
{
    public string? Reason { get; set; }
}
