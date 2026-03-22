namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>Fields available for sorting rental process lists.</summary>
public enum RentalSortField
{
    /// <summary>Sort by the last update timestamp (default).</summary>
    UpdatedAt,

    /// <summary>Sort by the creation timestamp.</summary>
    CreatedAt,

    /// <summary>Sort by the current workflow stage.</summary>
    Stage,

    /// <summary>Sort by the customer name on the linked rental.</summary>
    CustomerName
}
