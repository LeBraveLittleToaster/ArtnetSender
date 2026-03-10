using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using LumenForgeServer.Maintenance.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Maintenance.Service;

/// <summary>
/// Application service for maintenance backlog status operations.
/// </summary>
public class MaintenanceStatusService(IMaintenanceRepository repository)
{
    public async Task<MaintenanceStatusView> CreateStatus(CreateMaintenanceStatusDto dto, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var status = new MaintenanceBacklogStatus
        {
            Uuid = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await repository.AddStatusAsync(status, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return MaintenanceStatusView.FromEntity(status);
    }

    public async Task<MaintenanceStatusView> GetStatus(Guid uuid, CancellationToken ct)
    {
        var status = await repository.GetStatusByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance status '{uuid}' not found.");
        return MaintenanceStatusView.FromEntity(status);
    }

    public async Task<IReadOnlyList<MaintenanceStatusView>> ListStatuses(string? search, int limit, int offset, CancellationToken ct)
    {
        var statuses = await repository.ListStatusesAsync(search, limit, offset, ct);
        return statuses.Select(MaintenanceStatusView.FromEntity).ToList();
    }

    public async Task<MaintenanceStatusView> UpdateStatus(Guid uuid, UpdateMaintenanceStatusDto dto, CancellationToken ct)
    {
        var status = await repository.GetStatusByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance status '{uuid}' not found.");

        if (dto.Name is not null)
        {
            status.Name = dto.Name.Trim();
        }

        if (dto.Description is not null)
        {
            status.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        }

        status.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return MaintenanceStatusView.FromEntity(status);
    }

    public async Task DeleteStatus(Guid uuid, CancellationToken ct)
    {
        var status = await repository.GetStatusByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance status '{uuid}' not found.");

        var hasBacklogs = await repository.StatusHasBacklogsAsync(status.Id, ct);
        if (hasBacklogs)
        {
            throw new ValidationException(
                $"Cannot delete status '{status.Name}' because it has associated backlog entries.",
                new Dictionary<string, string[]> { ["status_uuid"] = [$"Status is in use by one or more backlog entries."] });
        }

        await repository.DeleteStatusAsync(status, ct);
        await repository.SaveChangesAsync(ct);
    }
}
