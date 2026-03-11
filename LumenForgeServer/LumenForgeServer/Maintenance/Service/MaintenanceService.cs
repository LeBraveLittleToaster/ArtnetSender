using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Persistance;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.Query;
using LumenForgeServer.Maintenance.Dto.View;
using LumenForgeServer.Maintenance.Persistence;
using InventoryBindingType = LumenForgeServer.Inventory.Domain.BindingType;
using InventoryDevice = LumenForgeServer.Inventory.Domain.Device;
using InventoryStockBinding = LumenForgeServer.Inventory.Domain.StockBinding;
using NodaTime;

namespace LumenForgeServer.Maintenance.Service;

/// <summary>
/// Application service for maintenance jobs, tasks and task logs.
/// </summary>
public class MaintenanceService(IMaintenanceRepository repository, IInventoryRepository inventoryRepository)
{
    private static readonly Duration DefaultMaintenanceBindingWindow = Duration.FromDays(3650);

    public async Task<MaintenanceJobView> CreateJob(CreateMaintenanceJobDto dto, string createdByUserKcId, CancellationToken ct)
    {
        if (dto.DeviceGuids.Count == 0)
        {
            throw new ValidationException("At least one device must be provided.",
                new Dictionary<string, string[]> { ["device_guids"] = ["At least one device guid is required."] });
        }

        var relatedJobs = await repository.GetJobsByGuidsAsync(dto.RelatedJobGuids, ct);
        if (relatedJobs.Count != dto.RelatedJobGuids.Distinct().Count())
        {
            throw new NotFoundException("One or more related jobs were not found.");
        }

        long? relatedRentalId = null;
        if (dto.RelatedRentalUuid.HasValue)
        {
            relatedRentalId = await repository.TryGetRentalIdByGuidAsync(dto.RelatedRentalUuid.Value, ct)
                ?? throw new NotFoundException($"Rental '{dto.RelatedRentalUuid}' not found.");
        }

        var affectedDevices = new List<InventoryDevice>();
        foreach (var deviceGuid in dto.DeviceGuids.Distinct())
        {
            var device = await inventoryRepository.GetDeviceByGuidAsync(deviceGuid, ct)
                ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");
            affectedDevices.Add(device);
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var job = new MaintenanceJob
        {
            Guid = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Status = MaintenanceStatus.Reported,
            CreatedByUserKcId = createdByUserKcId,
            RelatedToRentalId = relatedRentalId,
            AffectedDevices = affectedDevices,
            RelatedJobs = relatedJobs.ToList(),
            ReportedAt = now,
            UpdatedAt = now,
            ResolvedAt = null,
        };

        await repository.AddJobAsync(job, ct);
        await EnsureMaintenanceBindingsExist(job.AffectedDevices, now, ct);
        await repository.SaveChangesAsync(ct);

        var persisted = await repository.GetJobByGuidAsync(
                job.Guid,
                MaintenanceJobInclude.Devices | MaintenanceJobInclude.Tasks | MaintenanceJobInclude.Logs | MaintenanceJobInclude.RelatedJobs | MaintenanceJobInclude.RelatedRental,
                ct)
            ?? throw new NotFoundException("Maintenance job not found after creation.");

        return MaintenanceJobView.FromEntity(persisted);
    }

    public Task<MaintenanceJobView> GetJob(Guid jobGuid, CancellationToken ct)
        => GetJob(jobGuid, MaintenanceJobInclude.None, ct);

    public async Task<MaintenanceJobView> GetJob(Guid jobGuid, MaintenanceJobInclude include, CancellationToken ct)
    {
        var job = await repository.GetJobByGuidAsync(jobGuid, include, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");
        return MaintenanceJobView.FromEntity(job);
    }

    public Task<(IReadOnlyList<MaintenanceJobView> items, long total)> ListJobs(MaintenanceJobQueryDto query, CancellationToken ct)
        => ListJobs(query, MaintenanceJobInclude.None, ct);

    public async Task<(IReadOnlyList<MaintenanceJobView> items, long total)> ListJobs(
        MaintenanceJobQueryDto query,
        MaintenanceJobInclude include,
        CancellationToken ct)
    {
        var (items, total) = await repository.ListJobsAsync(
            query.Search,
            query.Status,
            query.UnresolvedOnly,
            query.Limit,
            query.Offset,
            include,
            ct);

        return (items.Select(MaintenanceJobView.FromEntity).ToList(), total);
    }

    public async Task<MaintenanceJobView> UpdateJob(Guid jobGuid, UpdateMaintenanceJobDto dto, CancellationToken ct)
    {
        var job = await repository.GetJobByGuidAsync(jobGuid, MaintenanceJobInclude.Devices | MaintenanceJobInclude.Tasks, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");

        if (dto.Name is not null)
        {
            job.Name = dto.Name.Trim();
        }

        if (dto.Description is not null)
        {
            job.Description = dto.Description.Trim();
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        if (dto.Status.HasValue)
        {
            job.Status = dto.Status.Value;
            if (IsTerminalStatus(dto.Status.Value))
            {
                job.ResolvedAt = now;
                await CloseActiveMaintenanceBindings(job.AffectedDevices, now, ct);
            }
            else
            {
                job.ResolvedAt = null;
                await EnsureMaintenanceBindingsExist(job.AffectedDevices, now, ct);
            }
        }

        job.UpdatedAt = now;
        await repository.SaveChangesAsync(ct);

        var updated = await repository.GetJobByGuidAsync(
                jobGuid,
                MaintenanceJobInclude.Devices | MaintenanceJobInclude.Tasks | MaintenanceJobInclude.Logs | MaintenanceJobInclude.RelatedJobs | MaintenanceJobInclude.RelatedRental,
                ct)
            ?? throw new NotFoundException("Maintenance job not found after update.");

        return MaintenanceJobView.FromEntity(updated);
    }

    public async Task DeleteJob(Guid jobGuid, CancellationToken ct)
    {
        var job = await repository.GetJobByGuidAsync(jobGuid, MaintenanceJobInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");

        await repository.DeleteJobAsync(job, ct);
        await repository.SaveChangesAsync(ct);
    }

    public Task<(IReadOnlyList<MaintenanceTaskView> items, long total)> ListTasks(Guid jobGuid, int limit, int offset, CancellationToken ct)
        => ListTasks(jobGuid, limit, offset, MaintenanceTaskInclude.None, ct);

    public async Task<(IReadOnlyList<MaintenanceTaskView> items, long total)> ListTasks(
        Guid jobGuid,
        int limit,
        int offset,
        MaintenanceTaskInclude include,
        CancellationToken ct)
    {
        _ = await repository.GetJobByGuidAsync(jobGuid, MaintenanceJobInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");

        var (tasks, total) = await repository.ListTasksForJobAsync(jobGuid, limit, offset, include, ct);
        return (tasks.Select(MaintenanceTaskView.FromEntity).ToList(), total);
    }

    public async Task<MaintenanceTaskView> CreateTask(Guid jobGuid, CreateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var job = await repository.GetJobByGuidAsync(jobGuid, MaintenanceJobInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");

        var affectedDevices = new List<InventoryDevice>();
        foreach (var deviceGuid in dto.AffectedDeviceGuids.Distinct())
        {
            var device = await inventoryRepository.GetDeviceByGuidAsync(deviceGuid, ct)
                ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");
            affectedDevices.Add(device);
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var task = new MaintenanceTask
        {
            Guid = Guid.NewGuid(),
            MaintenanceJobId = job.Id,
            Description = dto.Description.Trim(),
            Status = dto.Status,
            AssignedToUserKcId = string.IsNullOrWhiteSpace(dto.AssignedToUserKcId) ? null : dto.AssignedToUserKcId.Trim(),
            AffectedDevices = affectedDevices,
            CreatedAt = now,
            UpdatedAt = now,
            ResolvedAt = IsTerminalStatus(dto.Status) ? now : null,
        };

        await repository.AddTaskAsync(task, ct);
        await repository.SaveChangesAsync(ct);

        await SyncJobResolution(jobGuid, ct);

        var persisted = await repository.GetTaskByGuidAsync(task.Guid, MaintenanceTaskInclude.Devices | MaintenanceTaskInclude.Logs, ct)
            ?? throw new NotFoundException("Maintenance task not found after creation.");

        return MaintenanceTaskView.FromEntity(persisted);
    }

    public async Task<MaintenanceTaskView> UpdateTask(Guid jobGuid, Guid taskGuid, UpdateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var task = await repository.GetTaskByGuidAsync(taskGuid, MaintenanceTaskInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance task '{taskGuid}' not found.");

        if (task.MaintenanceJob.Guid != jobGuid)
        {
            throw new NotFoundException($"Task '{taskGuid}' is not part of job '{jobGuid}'.");
        }

        if (dto.Description is not null)
        {
            task.Description = dto.Description.Trim();
        }

        if (dto.AssignedToUserKcId is not null)
        {
            task.AssignedToUserKcId = string.IsNullOrWhiteSpace(dto.AssignedToUserKcId)
                ? null
                : dto.AssignedToUserKcId.Trim();
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        if (dto.Status.HasValue)
        {
            task.Status = dto.Status.Value;
            task.ResolvedAt = IsTerminalStatus(dto.Status.Value) ? now : null;
        }

        task.UpdatedAt = now;
        await repository.SaveChangesAsync(ct);

        await SyncJobResolution(jobGuid, ct);

        var updated = await repository.GetTaskByGuidAsync(taskGuid, MaintenanceTaskInclude.Devices | MaintenanceTaskInclude.Logs, ct)
            ?? throw new NotFoundException("Maintenance task not found after update.");

        return MaintenanceTaskView.FromEntity(updated);
    }

    public async Task DeleteTask(Guid jobGuid, Guid taskGuid, CancellationToken ct)
    {
        var task = await repository.GetTaskByGuidAsync(taskGuid, MaintenanceTaskInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance task '{taskGuid}' not found.");

        if (task.MaintenanceJob.Guid != jobGuid)
        {
            throw new NotFoundException($"Task '{taskGuid}' is not part of job '{jobGuid}'.");
        }

        await repository.DeleteTaskAsync(task, ct);
        await repository.SaveChangesAsync(ct);

        await SyncJobResolution(jobGuid, ct);
    }

    public async Task<IReadOnlyList<MaintenanceLogEntryView>> ListTaskLogs(Guid jobGuid, Guid taskGuid, CancellationToken ct)
    {
        var task = await repository.GetTaskByGuidAsync(taskGuid, MaintenanceTaskInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance task '{taskGuid}' not found.");

        if (task.MaintenanceJob.Guid != jobGuid)
        {
            throw new NotFoundException($"Task '{taskGuid}' is not part of job '{jobGuid}'.");
        }

        var logs = await repository.ListLogsForTaskAsync(taskGuid, ct);
        return logs.Select(MaintenanceLogEntryView.FromEntity).ToList();
    }

    public async Task<MaintenanceLogEntryView> AddTaskLog(Guid jobGuid, Guid taskGuid, CreateMaintenanceLogEntryDto dto, CancellationToken ct)
    {
        var task = await repository.GetTaskByGuidAsync(taskGuid, MaintenanceTaskInclude.None, ct)
            ?? throw new NotFoundException($"Maintenance task '{taskGuid}' not found.");

        if (task.MaintenanceJob.Guid != jobGuid)
        {
            throw new NotFoundException($"Task '{taskGuid}' is not part of job '{jobGuid}'.");
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var statusBefore = task.Status;
        var statusAfter = dto.StatusAfter ?? task.Status;

        var log = new MaintenanceLogEntry
        {
            Guid = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            MaintenanceTaskId = task.Id,
            StatusBefore = statusBefore,
            StatusAfter = statusAfter,
            CreatedAt = now,
        };

        if (statusAfter != statusBefore)
        {
            task.Status = statusAfter;
            task.ResolvedAt = IsTerminalStatus(statusAfter) ? now : null;
            task.UpdatedAt = now;
        }

        await repository.AddLogEntryAsync(log, ct);
        await repository.SaveChangesAsync(ct);

        await SyncJobResolution(jobGuid, ct);

        return MaintenanceLogEntryView.FromEntity(log);
    }

    private async Task SyncJobResolution(Guid jobGuid, CancellationToken ct)
    {
        var job = await repository.GetJobByGuidAsync(jobGuid, MaintenanceJobInclude.Tasks | MaintenanceJobInclude.Devices, ct)
            ?? throw new NotFoundException($"Maintenance job '{jobGuid}' not found.");

        var now = SystemClock.Instance.GetCurrentInstant();
        var hasTasks = job.Tasks.Count > 0;
        var allTerminal = hasTasks && job.Tasks.All(t => IsTerminalStatus(t.Status));

        if (allTerminal)
        {
            job.Status = MaintenanceStatus.Resolved;
            job.ResolvedAt = now;
            await CloseActiveMaintenanceBindings(job.AffectedDevices, now, ct);
        }
        else
        {
            if (job.Status == MaintenanceStatus.Resolved)
            {
                job.Status = MaintenanceStatus.UnderInvestigation;
            }

            job.ResolvedAt = null;
            await EnsureMaintenanceBindingsExist(job.AffectedDevices, now, ct);
        }

        job.UpdatedAt = now;
        await repository.SaveChangesAsync(ct);
    }

    private static bool IsTerminalStatus(MaintenanceStatus status)
        => status is MaintenanceStatus.Resolved or MaintenanceStatus.NotResolvable or MaintenanceStatus.NoMaintenanceNeeded;

    private async Task EnsureMaintenanceBindingsExist(IEnumerable<InventoryDevice> devices, Instant start, CancellationToken ct)
    {
        foreach (var device in devices)
        {
            var end = start + DefaultMaintenanceBindingWindow;
            var hasActive = await inventoryRepository.HasConflictingBindingsAsync(device.Id, start, end, InventoryBindingType.MAINTENANCE, ct);
            if (hasActive)
            {
                continue;
            }

            await inventoryRepository.AddStockBindingAsync(new InventoryStockBinding
            {
                Guid = Guid.NewGuid(),
                DeviceId = device.Id,
                BindingType = InventoryBindingType.MAINTENANCE,
                CreatedAt = start,
                Start = start,
                End = end,
            }, ct);
        }
    }

    private async Task CloseActiveMaintenanceBindings(IEnumerable<InventoryDevice> devices, Instant resolvedAt, CancellationToken ct)
    {
        foreach (var device in devices)
        {
            var bindings = await inventoryRepository.GetStockBindingsByDeviceIdAsync(device.Id, ct);
            foreach (var binding in bindings.Where(b =>
                         b.BindingType == InventoryBindingType.MAINTENANCE &&
                         b.Start < resolvedAt &&
                         b.End > resolvedAt))
            {
                binding.End = resolvedAt;
            }
        }
    }
}
