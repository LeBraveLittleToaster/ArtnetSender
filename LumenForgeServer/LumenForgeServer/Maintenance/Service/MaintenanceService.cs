using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Domain;
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
    private static readonly Duration DefaultMaintenanceBindingWindow = Duration.FromDays(3650);

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

        if (deviceId.HasValue)
        {
            await EnsureMaintenanceBindingExists(deviceId.Value, now, ct);
        }

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

        var now = SystemClock.Instance.GetCurrentInstant();

        if (dto.Resolve == true && backlog.ResolvedAt is null)
        {
            backlog.ResolvedAt = now;
            if (backlog.DeviceId.HasValue)
            {
                await CloseActiveMaintenanceBindings(backlog.DeviceId.Value, now, ct);
            }
        }
        else if (dto.Resolve == false)
        {
            backlog.ResolvedAt = null;
            if (backlog.DeviceId.HasValue)
            {
                await EnsureMaintenanceBindingExists(backlog.DeviceId.Value, now, ct);
            }
        }

        backlog.UpdatedAt = now;
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

    private async Task EnsureMaintenanceBindingExists(long deviceId, Instant start, CancellationToken ct)
    {
        var end = start + DefaultMaintenanceBindingWindow;
        var hasActiveMaintenanceBinding = await inventoryRepository.HasConflictingBindingsAsync(
            deviceId,
            start,
            end,
            BindingType.MAINTENANCE,
            ct);

        if (hasActiveMaintenanceBinding)
        {
            return;
        }

        await inventoryRepository.AddStockBindingAsync(new StockBinding
        {
            Guid = Guid.NewGuid(),
            DeviceId = deviceId,
            BindingType = BindingType.MAINTENANCE,
            CreatedAt = start,
            Start = start,
            End = end,
        }, ct);
    }

    private async Task CloseActiveMaintenanceBindings(long deviceId, Instant resolvedAt, CancellationToken ct)
    {
        var bindings = await inventoryRepository.GetStockBindingsByDeviceIdAsync(deviceId, ct);

        foreach (var binding in bindings.Where(b =>
                     b.BindingType == BindingType.MAINTENANCE &&
                     b.Start < resolvedAt &&
                     b.End > resolvedAt))
        {
            binding.End = resolvedAt;
        }
    }
}
