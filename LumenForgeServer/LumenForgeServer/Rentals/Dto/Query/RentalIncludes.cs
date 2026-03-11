namespace LumenForgeServer.Rentals.Dto.Query;

[Flags]
public enum RentalInclude
{
    None = 0,
    Items = 1,
    Checklists = 2,
    Invoices = 4,
    Events = 8,
    Extensions = 16,
    Report = 32,
}
