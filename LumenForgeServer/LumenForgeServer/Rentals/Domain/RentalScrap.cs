using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Captures whether and when a rental was scrapped, and by whom.
/// </summary>
public class RentalScrap
{
    public bool IsScrapped { get; set; }
    public Instant? ScrappedAt { get; set; }
    public string? ScrappedByUserId { get; set; }
}
