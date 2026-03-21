using LumenForgeServer.Common.Database;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRentalProcessRepository"/>.
/// Scoped to the current <see cref="AppDbContext"/> unit of work.
/// </summary>
public class RentalProcessRepository(AppDbContext db) : IRentalProcessRepository
{
    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidAsync(Guid processGuid, CancellationToken ct)
    {
        return await db.RentalProcessInstances
            .Include(p => p.Rental)
            .FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidWithDetailsAsync(Guid processGuid, CancellationToken ct)
    {
        return await db.RentalProcessInstances
            .Include(p => p.Rental)
            .Include(p => p.Checklists).ThenInclude(c => c.Items)
            .Include(p => p.Extensions)
            .Include(p => p.DamageReports)
            .FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(RentalProcessInstance instance, CancellationToken ct)
    {
        await db.RentalProcessInstances.AddAsync(instance, ct);
    }

    /// <inheritdoc />
    public Task UpdateAsync(RentalProcessInstance instance, CancellationToken ct)
    {
        db.RentalProcessInstances.Update(instance);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddActionLogAsync(RentalActionLog log, CancellationToken ct)
    {
        await db.RentalActionLogs.AddAsync(log, ct);
    }

    /// <inheritdoc />
    public async Task AddRentalAsync(Rental rental, CancellationToken ct)
    {
        await db.Rentals.AddAsync(rental, ct);
    }

    /// <inheritdoc />
    public async Task AddChecklistAsync(Checklist checklist, CancellationToken ct)
    {
        await db.Checklists.AddAsync(checklist, ct);
    }

    /// <inheritdoc />
    public async Task<Checklist?> GetChecklistByGuidAsync(Guid checklistGuid, CancellationToken ct)
    {
        return await db.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Guid == checklistGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddExtensionAsync(RentalExtension extension, CancellationToken ct)
    {
        await db.RentalExtensions.AddAsync(extension, ct);
    }

    /// <inheritdoc />
    public async Task<RentalExtension?> GetExtensionByGuidAsync(Guid extensionGuid, CancellationToken ct)
    {
        return await db.RentalExtensions
            .FirstOrDefaultAsync(e => e.Guid == extensionGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddDamageReportsAsync(IEnumerable<RentalDamageReport> reports, CancellationToken ct)
    {
        await db.RentalDamageReports.AddRangeAsync(reports, ct);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
