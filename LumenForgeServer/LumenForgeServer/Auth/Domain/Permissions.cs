using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Domain;

/// <summary>
/// Authorization roles used throughout the application.
/// </summary>
/// <remarks>
/// Values are grouped by domain area and numeric range to keep role families distinct.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Permissions
{
    // =========================
    // Inventory
    // =========================

    // Device (10–19)
    DeviceCreate = 10,
    DeviceRead = 11,
    DeviceUpdate = 12,
    DeviceDelete = 13,

    // Vendor (20–29)
    VendorCreate = 20,
    VendorRead = 21,
    VendorUpdate = 22,
    VendorDelete = 23,

    // Category (30–39)
    CategoryCreate = 30,
    CategoryRead = 31,
    CategoryUpdate = 32,
    CategoryDelete = 33,

    // Stock (40–49)
    StockCreate = 40,
    StockRead = 41,
    StockUpdate = 42,
    StockDelete = 43,

    // =========================
    // Maintenance
    // =========================

    // Backlog (50–59)
    MaintenanceCreate = 50,
    MaintenanceRead = 51,
    MaintenanceUpdate = 52,
    MaintenanceDelete = 53,

    // =========================
    // Rentals
    // =========================

    // Rental (60–69)
    RentalCreate = 60,
    RentalReadAll = 61,
    RentalUpdateAll = 62,
    RentalDeleteAll = 63,
    RentalUserOwn = 64,
    RentalGroup = 65,

    // Rental Action (500–549)
    RentalActionCreateRental = 500,
    RentalActionApproveRequest = 501,
    RentalActionRejectRequest = 502,
    RentalActionAssignItems = 503,
    RentalActionRemoveItems = 504,
    RentalActionApproveItems = 505,
    RentalActionRejectItems = 506,
    RentalActionGenerateChecklist = 507,
    RentalActionScanChecklist = 508,
    RentalActionSignChecklist = 509,
    RentalActionRecordPickup = 510,
    RentalActionRecordReturn = 511,
    RentalActionRequestExtension = 512,
    RentalActionApproveExtension = 513,
    RentalActionRejectExtension = 514,
    RentalActionRecordDamages = 515,
    RentalActionCreateMaintenanceJobs = 516,
    RentalActionGenerateInvoice = 517,
    RentalActionRecordPayment = 518,
    RentalActionGenerateReport = 519,
    RentalActionCompleteRental = 520,
    RentalActionCancelRental = 521,
    RentalActionScrapRental = 522,

    // =========================
    // Billing
    // =========================

    // Invoice (80–89)
    InvoiceCreate = 80,
    InvoiceRead = 81,
    InvoiceUpdate = 82,
    InvoiceDelete = 83,

    // Invoice Status (90–99)
    InvoiceStatusCreate = 90,
    InvoiceStatusRead = 91,
    InvoiceStatusUpdate = 92,
    InvoiceStatusDelete = 93,

    // Catalogue (110–119)
    CatalogueCreate = 110,
    CatalogueRead = 111,
    CatalogueUpdate = 112,
    CatalogueDelete = 113,

    // Roles
    RoleRead = 101,
    RoleUpdate = 102,
    RoleDelete = 103,

    // Groups
    GroupCreate = 200,
    GroupRead = 201,
    GroupUpdate = 202,
    GroupDelete = 203,

    // Users
    UserCreate = 300,
    UserRead = 301,
    UserUpdate = 302,
    UserDelete = 303,
}
