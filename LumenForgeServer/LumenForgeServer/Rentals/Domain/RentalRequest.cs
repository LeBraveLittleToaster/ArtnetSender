using LumenForgeServer.Common;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Customer-provided request content for a rental.
/// </summary>
public class RentalRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? EventName { get; set; }
    public string? CustomerNotes { get; set; }
    public string? DeliveryAddress { get; set; }
    public RentalPriority Priority { get; set; } = RentalPriority.NORMAL;
}
