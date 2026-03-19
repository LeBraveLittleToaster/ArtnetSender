namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a rental extension was rejected by staff.</summary>
public sealed class RejectExtensionAction : RentalAction
{
    public long ExtensionId { get; set; }
    public RentalExtension Extension { get; set; } = null!;

    public string? Reason { get; set; }
}
