namespace LumenForgeServer.Rentals.Domain.Actions;

/// <summary>Records that a rental was scrapped. Status → Scrapped.</summary>
public sealed class ScrapRentalAction : RentalAction
{
    public string? Reason { get; set; }
}
