using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Common.Database;

/// <summary>
/// EF Core DbContext that maps all application modules to database tables.
/// </summary>
/// <remarks>
/// This context owns schema configuration for auth, inventory, billing, maintenance, and rentals.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Authentication and Authorization
    /// <summary>
    /// Auth users keyed by Keycloak subject id.
    /// </summary>
    public DbSet<KcUserReference> Users => Set<KcUserReference>();
    /// <summary>
    /// Auth groups used to assign roles to users.
    /// </summary>
    public DbSet<Group> Groups => Set<Group>();
    /// <summary>
    /// Join table linking groups to roles.
    /// </summary>
    public DbSet<GroupPermissions> GroupRoles => Set<GroupPermissions>();
    /// <summary>
    /// Join table linking users to groups.
    /// </summary>
    public DbSet<GroupUser> GroupUsers => Set<GroupUser>();

    // Inventory
    /// <summary>
    /// Inventory vendors.
    /// </summary>
    public DbSet<Vendor> Vendors => Set<Vendor>();
    /// <summary>
    /// Inventory categories used to classify devices.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();
    /// <summary>
    /// Maintenance status entries for devices.
    /// </summary>
    public DbSet<MaintenanceStatus> MaintenanceStatuses => Set<MaintenanceStatus>();
    /// <summary>
    /// Inventory devices.
    /// </summary>
    public DbSet<Device> Devices => Set<Device>();
    /// <summary>
    /// Stock entries tied to devices.
    /// </summary>
    public DbSet<Stock> Stocks => Set<Stock>();
    /// <summary>
    /// Device parameter entries.
    /// </summary>
    public DbSet<DeviceParameter> DeviceParameters => Set<DeviceParameter>();
    /// <summary>
    /// Join table linking devices to categories.
    /// </summary>
    public DbSet<DeviceCategory> DeviceCategories => Set<DeviceCategory>();

    // Billing
    /// <summary>
    /// Invoice status lookup table.
    /// </summary>
    public DbSet<InvoiceStatus> InvoiceStatuses => Set<InvoiceStatus>();
    /// <summary>
    /// Invoice records.
    /// </summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();
    /// <summary>
    /// Payment status lookup table.
    /// </summary>
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    /// <summary>
    /// Payment records.
    /// </summary>
    public DbSet<Payment> Payments => Set<Payment>();

    // Maintenance
    /// <summary>
    /// Maintenance backlog status lookup table.
    /// </summary>
    public DbSet<MaintenanceBacklogStatus> MaintenanceBacklogStatuses => Set<MaintenanceBacklogStatus>();
    /// <summary>
    /// Maintenance backlog entries.
    /// </summary>
    public DbSet<MaintenanceBacklog> MaintenanceBacklogs => Set<MaintenanceBacklog>();

    // Rentals
    /// <summary>
    /// Rental status lookup table.
    /// </summary>
    public DbSet<RentalStatus> RentalStatuses => Set<RentalStatus>();
    /// <summary>
    /// Rental records.
    /// </summary>
    public DbSet<Rental> Rentals => Set<Rental>();
    /// <summary>
    /// Rental line items.
    /// </summary>
    public DbSet<RentalItem> RentalItems => Set<RentalItem>();
    /// <summary>
    /// Checklists associated with rentals.
    /// </summary>
    public DbSet<Checklist> Checklists => Set<Checklist>();
    /// <summary>
    /// Checklist line items.
    /// </summary>
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    /// <summary>
    /// Rental report records.
    /// </summary>
    public DbSet<RentalReport> RentalReports => Set<RentalReport>();

    /// <summary>
    /// Configures the entity schema for all modules.
    /// </summary>
    /// <param name="b">Model builder used to configure entity mappings.</param>
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.ApplyModuleConfigurations();
    }
}
