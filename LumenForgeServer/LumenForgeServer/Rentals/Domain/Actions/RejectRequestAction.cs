namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records rejection of a rental request. Status → Rejected.</summary>
public sealed class RejectRequestAction : RentalAction
{
    public string Reason { get; set; } = null!;
}
