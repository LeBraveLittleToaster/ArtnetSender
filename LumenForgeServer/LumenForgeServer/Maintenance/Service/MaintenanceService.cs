using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Persistance;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.Query;
using LumenForgeServer.Maintenance.Dto.View;
using LumenForgeServer.Maintenance.Persistence;
using NodaTime;

namespace LumenForgeServer.Maintenance.Service;

/// <summary>
/// Application service for maintenance backlog operations.
/// </summary>
public class MaintenanceService(IMaintenanceRepository repository, IInventoryRepository inventoryRepository)
{
    public async Task<MaintenanceBacklogView> CreateBacklog(CreateMaintenanceBacklogDto dto, CancellationToken ct)
    {
        if (dto.DeviceUuid is null && dto.RentalItemUuid is null)
        {
            throw new ValidationException(
                "At least one of device_uuid or rental_item_uuid must be provided.",
                new Dictionary<string, string[]>
                {
                    ["device_uuid"] = ["Either device_uuid or rental_item_uuid is required."],
                    ["rental_item_uuid"] = ["Either device_uuid or rental_item_uuid is required."],
                });
        }

        var statusId = await repository.TryGetStatusIdByUuidAsync(dto.StatusUuid, ct)
            ?? throw new NotFoundException($"Maintenance status '{dto.StatusUuid}' not found.");

        long? deviceId = null;
        if (dto.DeviceUuid.HasValue)
        {
            deviceId = await inventoryRepository.TryGetDeviceIdByGuidAsync(dto.DeviceUuid.Value, ct)
                ?? throw new NotFoundException($"Device '{dto.DeviceUuid}' not found.");
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var backlog = new MaintenanceBacklog
        {
            Uuid = Guid.NewGuid(),
            MaintenanceBacklogStatusId = statusId,
            DeviceId = deviceId,
            IssueSummary = dto.IssueSummary.Trim(),
            IssueDescription = dto.IssueDescription?.Trim(),
            QuantityAffected = dto.QuantityAffected,
            ReportedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddBacklogAsync(backlog, ct);
        await repository.SaveChangesAsync(ct);

        var persisted = await repository.GetBacklogByUuidAsync(backlog.Uuid, ct)
            ?? throw new NotFoundException("Backlog entry not found after creation.");

        return MaintenanceBacklogView.FromEntity(persisted);
    }

    public async Task<MaintenanceBacklogView> GetBacklog(Guid uuid, CancellationToken ct)
    {
        var backlog = await repository.GetBacklogByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance backlog '{uuid}' not found.");
        return MaintenanceBacklogView.FromEntity(backlog);
    }

    public async Task<(IReadOnlyList<MaintenanceBacklogView> items, long total)> ListBacklogs(
        MaintenanceQueryDto query, CancellationToken ct)
    {
        var (items, total) = await repository.ListBacklogsAsync(
            query.Search, query.StatusUuid, query.UnresolvedOnly, query.Limit, query.Offset, ct);
        return (items.Select(MaintenanceBacklogView.FromEntity).ToList(), total);
    }

    public async Task<IReadOnlyList<MaintenanceBacklogView>> GetBacklogsByDevice(Guid deviceGuid, CancellationToken ct)
    {
        var deviceId = await inventoryRepository.TryGetDeviceIdByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

        var items = await repository.GetBacklogsByDeviceIdAsync(deviceId, ct);
        return items.Select(MaintenanceBacklogView.FromEntity).ToList();
    }

    public async Task<MaintenanceBacklogView> UpdateBacklog(Guid uuid, UpdateMaintenanceBacklogDto dto, CancellationToken ct)
    {
        var backlog = await repository.GetBacklogByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance backlog '{uuid}' not found.");

        if (dto.StatusUuid.HasValue)
        {
            backlog.MaintenanceBacklogStatusId = await repository.TryGetStatusIdByUuidAsync(dto.StatusUuid.Value, ct)
                ?? throw new NotFoundException($"Maintenance status '{dto.StatusUuid}' not found.");
        }

        if (dto.IssueSummary is not null)
        {
            backlog.IssueSummary = dto.IssueSummary.Trim();
        }

        if (dto.IssueDescription is not null)
        {
            backlog.IssueDescription = string.IsNullOrWhiteSpace(dto.IssueDescription) ? null : dto.IssueDescription.Trim();
        }

        if (dto.QuantityAffected.HasValue)
        {
            backlog.QuantityAffected = dto.QuantityAffected.Value;
        }

        if (dto.Resolve == true && backlog.ResolvedAt is null)
        {
            backlog.ResolvedAt = SystemClock.Instance.GetCurrentInstant();
        }
        else if (dto.Resolve == false)
        {
            backlog.ResolvedAt = null;
        }

        backlog.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        await repository.SaveChangesAsync(ct);

        var updated = await repository.GetBacklogByUuidAsync(uuid, ct)
            ?? throw new NotFoundException("Backlog entry not found after update.");

        return MaintenanceBacklogView.FromEntity(updated);
    }

    public async Task DeleteBacklog(Guid uuid, CancellationToken ct)
    {
        var backlog = await repository.GetBacklogByUuidAsync(uuid, ct)
            ?? throw new NotFoundException($"Maintenance backlog '{uuid}' not found.");

        await repository.DeleteBacklogAsync(backlog, ct);
        await repository.SaveChangesAsync(ct);
    }
}
