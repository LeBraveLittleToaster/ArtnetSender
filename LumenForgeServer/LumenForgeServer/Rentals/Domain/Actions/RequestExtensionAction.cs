namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a rental extension was requested by the customer.</summary>
public sealed class RequestExtensionAction : RentalAction
{
    public long ExtensionId { get; set; }
    public RentalExtension Extension { get; set; } = null!;
}
