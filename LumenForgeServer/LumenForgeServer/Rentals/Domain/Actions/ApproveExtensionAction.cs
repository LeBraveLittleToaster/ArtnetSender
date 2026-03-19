namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a rental extension was approved by staff.</summary>
public sealed class ApproveExtensionAction : RentalAction
{
    public long ExtensionId { get; set; }
    public RentalExtension Extension { get; set; } = null!;
}
