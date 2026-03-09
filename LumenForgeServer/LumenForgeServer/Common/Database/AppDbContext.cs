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
    public DbSet<StockBinding> StockBindings => Set<StockBinding>();
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

        AddAuthModuleTableDef(b);
        AddInventoryModuleTableDef(b);
        AddBillingModuleTableDef(b);
        AddmaintenanceModuleTableDef(b);
        AddRentalModuleTableDef(b);
    }

    /// <summary>
    /// Configures billing module table mappings.
    /// </summary>
    /// <param name="builder">Model builder used to configure entity mappings.</param>
    private static void AddBillingModuleTableDef(ModelBuilder builder)
    {
        builder.Entity<InvoiceStatus>().ToTable("invoice_status");
        builder.Entity<Invoice>().ToTable("invoice");
        builder.Entity<PaymentStatus>().ToTable("payment_status");
        builder.Entity<Payment>().ToTable("payment");

        builder.Entity<InvoiceStatus>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.InvoiceNumber).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.InvoiceNumber).IsUnique();

            e.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);

            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.InvoiceDocumentUrl).HasMaxLength(2000);

            e.Property(x => x.GeneratedByUserId).HasMaxLength(128);

            e.HasOne(x => x.Rental)
                .WithMany(r => r.Invoices)
                .HasForeignKey(x => x.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.InvoiceStatus)
                .WithMany(s => s.Invoices)
                .HasForeignKey(x => x.InvoiceStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentStatus>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.ProviderReference).HasMaxLength(256);

            e.HasOne(x => x.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.PaymentStatus)
                .WithMany(s => s.Payments)
                .HasForeignKey(x => x.PaymentStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures maintenance module table mappings.
    /// </summary>
    /// <param name="b">Model builder used to configure entity mappings.</param>
    private static void AddmaintenanceModuleTableDef(ModelBuilder b)
    {
        b.Entity<MaintenanceBacklogStatus>().ToTable("maintenance_backlog_status");
        b.Entity<MaintenanceBacklog>().ToTable("maintenance_backlog");

        b.Entity<MaintenanceBacklogStatus>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<MaintenanceBacklog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.QuantityAffected).HasPrecision(18, 3);
            e.Property(x => x.IssueSummary).HasMaxLength(2000).IsRequired();
            e.Property(x => x.IssueDescription).HasMaxLength(4000);

            e.HasOne(x => x.RentalItem)
                .WithMany(ri => ri.MaintenanceBacklogs)
                .HasForeignKey(x => x.RentalItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ChecklistItem)
                .WithMany(ci => ci.MaintenanceBacklogs)
                .HasForeignKey(x => x.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.MaintenanceBacklogStatus)
                .WithMany(s => s.MaintenanceBacklogs)
                .HasForeignKey(x => x.MaintenanceBacklogStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configures rentals module table mappings.
    /// </summary>
    /// <param name="builder">Model builder used to configure entity mappings.</param>
    private static void AddRentalModuleTableDef(ModelBuilder builder)
    {
        builder.Entity<RentalStatus>().ToTable("rental_status");
        builder.Entity<Rental>().ToTable("rental");
        builder.Entity<RentalItem>().ToTable("rental_item");
        builder.Entity<Checklist>().ToTable("checklist");
        builder.Entity<ChecklistItem>().ToTable("checklist_item");
        builder.Entity<RentalReport>().ToTable("rental_report");

        builder.Entity<RentalStatus>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Rental>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.CustomerUserId).HasMaxLength(128).IsRequired();
            e.Property(x => x.AssignedByUserId).HasMaxLength(128);
            e.Property(x => x.PickupProcessedByUserId).HasMaxLength(128);
            e.Property(x => x.DropoffProcessedByUserId).HasMaxLength(128);
            e.Property(x => x.CompletedByUserId).HasMaxLength(128);
            e.Property(x => x.ScrappedByUserId).HasMaxLength(128);

            e.HasIndex(x => x.CustomerUserId);

            e.Property(x => x.RequestTitle).HasMaxLength(512);
            e.Property(x => x.RequestDescription).HasMaxLength(4000);

            e.HasOne(x => x.RentalStatus)
                .WithMany(s => s.Rentals)
                .HasForeignKey(x => x.RentalStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RentalReport)
                .WithOne(rr => rr.Rental)
                .HasForeignKey<RentalReport>(rr => rr.RentalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RentalItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.ConditionNotes).HasMaxLength(4000);
            e.Property(x => x.ApprovedByUserId).HasMaxLength(128);

            e.HasOne(x => x.Rental)
                .WithMany(r => r.Items)
                .HasForeignKey(x => x.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

           
        });

        builder.Entity<Checklist>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.ChecklistType).HasConversion<string>().HasMaxLength(32).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(4000);

            e.Property(x => x.GeneratedByUserId).HasMaxLength(128);
            e.Property(x => x.SignedByUserId).HasMaxLength(128);

            e.HasOne(x => x.Rental)
                .WithMany(r => r.Checklists)
                .HasForeignKey(x => x.RentalId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.SourceChecklist)
                .WithMany(x => x.DerivedChecklists)
                .HasForeignKey(x => x.SourceChecklistId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.RentalId, x.ChecklistType }).IsUnique(false);
        });

        builder.Entity<ChecklistItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();

            e.Property(x => x.QuantityChecked).HasPrecision(18, 3);
            e.Property(x => x.DamagedQuantity).HasPrecision(18, 3);

            e.Property(x => x.ConditionNotes).HasMaxLength(4000);
            e.Property(x => x.DamageSummary).HasMaxLength(2000);
            e.Property(x => x.DamageDescription).HasMaxLength(4000);

            e.HasOne(x => x.Checklist)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RentalItem)
                .WithMany(ri => ri.ChecklistItems)
                .HasForeignKey(x => x.RentalItemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ChecklistId, x.RentalItemId }).IsUnique();
        });

        builder.Entity<RentalReport>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.RentalId).IsUnique();

            e.Property(x => x.GeneratedByUserId).HasMaxLength(128);

            e.Property(x => x.ReportSummary).HasMaxLength(4000);
            e.Property(x => x.ReportDocumentUrl).HasMaxLength(2000);
        });
    }

    /// <summary>
    /// Configures inventory module table mappings.
    /// </summary>
    /// <param name="builder">Model builder used to configure entity mappings.</param>
    private static void AddInventoryModuleTableDef(ModelBuilder builder)
    {
        builder.Entity<Vendor>().ToTable("vendor");
        builder.Entity<Category>().ToTable("category");
        builder.Entity<MaintenanceStatus>().ToTable("maintenance_status");
        builder.Entity<Device>().ToTable("device");
        builder.Entity<StockBinding>().ToTable("stock_binding");
        builder.Entity<DeviceParameter>().ToTable("device_parameter");
        builder.Entity<DeviceCategory>().ToTable("device_category");

        builder.Entity<Vendor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Guid).IsUnique();

            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Guid).IsUnique();
        });

        builder.Entity<MaintenanceStatus>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasIndex(x => x.Uuid).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Device>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SerialNumber).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Guid).IsUnique();
            e.HasIndex(x => x.SerialNumber).IsUnique();

            e.Property(x => x.DeviceName).HasMaxLength(512);
            e.Property(x => x.DeviceDescription).HasMaxLength(4000);
            e.Property(x => x.PhotoUrl).HasMaxLength(2000);

            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.Property(x => x.PurchaseDate);

            e.HasOne(x => x.Vendor)
                .WithMany(v => v.Devices)
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.MaintenanceStatus)
                .WithMany(ms => ms.Devices)
                .HasForeignKey(x => x.MaintenanceStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.StockBindings)
                .WithOne(s => s.Device)
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StockBinding>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Guid).IsUnique();
            e.HasIndex(x => x.DeviceId);

            e.Property(x => x.BindingType).IsRequired();
            e.Property(x => x.Start).IsRequired();

            e.Property(x => x.End).IsRequired();

        });

        builder.Entity<DeviceParameter>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ParamKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.Value).HasMaxLength(4000).IsRequired();

            e.HasOne(x => x.Device)
                .WithMany(d => d.Parameters)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.DeviceId, x.ParamKey }).IsUnique();
        });

        builder.Entity<DeviceCategory>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasOne(x => x.Device)
                .WithMany(d => d.DeviceCategories)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
                .WithMany(c => c.DeviceCategories)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.DeviceId, x.CategoryId }).IsUnique();
        });
    }

    /// <summary>
    /// Configures auth module table mappings.
    /// </summary>
    /// <param name="builder">Model builder used to configure entity mappings.</param>
    private static void AddAuthModuleTableDef(ModelBuilder builder)
    {
        builder.Entity<KcUserReference>().ToTable("users_kc_reference");
        builder.Entity<Group>().ToTable("groups");
        builder.Entity<GroupPermissions>().ToTable("group_roles");
        builder.Entity<GroupUser>().ToTable("group_users");

        builder.Entity<KcUserReference>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserKcId).IsUnique();

            e.HasMany(x => x.GroupUsers)
                .WithOne(x => x.KcUserReference)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Group>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Guid).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();

            e.HasMany(x => x.GroupRoles)
                .WithOne(x => x.Group)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.GroupUsers)
                .WithOne(x => x.Group)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GroupPermissions>(e =>
        {
            e.HasKey(x => new { x.GroupId, RoleId = x.Permission });
            e.Property(x => x.Permission).HasConversion<int>();

        });

        builder.Entity<GroupUser>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.UserId });
            e.HasIndex(x => x.UserId);
        });
    }
}
