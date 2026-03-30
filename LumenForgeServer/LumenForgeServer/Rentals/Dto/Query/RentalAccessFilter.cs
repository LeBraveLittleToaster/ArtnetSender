namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>
/// Internal read/update access scope for rental queries.
/// </summary>
public sealed record RentalAccessFilter
{
    public static RentalAccessFilter AllowAllScope { get; } = new()
    {
        AllowAll = true
    };

    public bool AllowAll { get; init; }

    public string? OwnerKcId { get; init; }

    public IReadOnlyList<Guid> GroupGuids { get; init; } = [];

    public bool HasAnyScope =>
        AllowAll ||
        !string.IsNullOrWhiteSpace(OwnerKcId) ||
        GroupGuids.Count > 0;
}
