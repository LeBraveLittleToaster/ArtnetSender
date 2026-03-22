namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>Flags controlling which related entities to include when loading a rental process.</summary>
[Flags]
public enum RentalProcessInclude
{
    /// <summary>No related entities beyond the base process and rental data.</summary>
    None = 0,

    /// <summary>Include checklists with their items.</summary>
    Checklists = 1,

    /// <summary>Include extension requests.</summary>
    Extensions = 2,

    /// <summary>Include damage reports.</summary>
    DamageReports = 4,

    /// <summary>Shorthand for all related entities.</summary>
    All = Checklists | Extensions | DamageReports
}
