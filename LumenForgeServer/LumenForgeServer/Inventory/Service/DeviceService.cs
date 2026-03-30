using LumenForgeServer.Common;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Factory;
using LumenForgeServer.Inventory.Persistance;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Inventory.Service;

/// <summary>
/// Application service for device operations.
/// </summary>
public class DeviceService(IInventoryRepository repository)
{
    /// <summary>
    /// Executes the create device operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceView result.</returns>
    public async Task<DeviceView> CreateDevice(CreateDeviceDto dto, CancellationToken ct)
    {
        EnsureNonEmptyGuid(dto.VendorGuid, nameof(dto.VendorGuid));

        var vendorId = await repository.TryGetVendorIdByGuidAsync(dto.VendorGuid, ct)
            ?? throw new NotFoundException($"Vendor '{dto.VendorGuid}' not found.");

        var categoryIds = await ResolveCategoryIds(dto.CategoryGuids, ct);
        var maintenanceStatusId = await ResolveMaintenanceStatusIdForCreate(dto.MaintenanceStatusUuid, ct);

        var device = DeviceFactory.Create(dto, vendorId, maintenanceStatusId, categoryIds);

        try
        {
            await repository.AddDeviceAsync(device, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        var persisted = await repository.GetDeviceByGuidAsync(device.Guid, ct)
            ?? throw new NotFoundException("Device not found after creation.");

        return DeviceView.FromEntity(persisted);
    }

    /// <summary>
    /// Executes the get device operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceView result.</returns>
    public async Task<DeviceView> GetDevice(Guid deviceGuid, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        return DeviceView.FromEntity(device);
    }

    /// <summary>
    /// Executes the task operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<DeviceView> devices, long total)> ListDevices(string? search, int limit, int offset, CancellationToken ct)
    {
        var (devices, total) = await repository.ListDevicesAsync(search, limit, offset, ct);
        return (devices.Select(DeviceView.FromEntity).ToList(), total);
    }

    /// <summary>
    /// Executes the update device operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceView result.</returns>
    public async Task<DeviceView> UpdateDevice(Guid deviceGuid, UpdateDeviceDto dto, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        if (dto.SerialNumber is not null)
        {
            device.SerialNumber = dto.SerialNumber;
        }

        if (dto.Name is not null)
        {
            device.DeviceName = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name;
        }

        if (dto.Description is not null)
        {
            device.DeviceDescription = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
        }

        if (dto.PhotoUrl is not null)
        {
            device.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl;
        }

        if (dto.VendorGuid is not null)
        {
            EnsureNonEmptyGuid(dto.VendorGuid.Value, nameof(dto.VendorGuid));

            var vendorId = await repository.TryGetVendorIdByGuidAsync(dto.VendorGuid.Value, ct)
                ?? throw new NotFoundException($"Vendor '{dto.VendorGuid}' not found.");

            device.VendorId = vendorId;
        }

        if (dto.MaintenanceStatusUuid is not null)
        {
            EnsureNonEmptyGuid(dto.MaintenanceStatusUuid.Value, nameof(dto.MaintenanceStatusUuid));

            var maintenanceStatusId = await repository.TryGetMaintenanceStatusIdByGuidAsync(dto.MaintenanceStatusUuid.Value, ct)
                ?? throw new NotFoundException($"Maintenance status '{dto.MaintenanceStatusUuid}' not found.");

            device.MaintenanceStatusId = maintenanceStatusId;
        }

        if (dto.PurchasePrice is not null)
        {
            device.PurchasePrice = dto.PurchasePrice.Value;
        }

        if (dto.PurchaseDate is not null)
        {
            device.PurchaseDate = dto.PurchaseDate.Value;
        }

        if (dto.StockUnitType is not null)
        {
            device.StockUnitType = dto.StockUnitType.Value;
        }

        if (dto.StockAmount is not null)
        {
            device.StockAmount = dto.StockAmount.Value;
        }

        device.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        var updated = await repository.GetDeviceByGuidAsync(device.Guid, ct)
            ?? throw new NotFoundException("Device not found after update.");

        return DeviceView.FromEntity(updated);
    }

    /// <summary>
    /// Executes the delete device operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteDevice(Guid deviceGuid, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        await repository.DeleteDeviceAsync(device, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Executes the set device categories operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceView result.</returns>
    public async Task<DeviceView> SetDeviceCategories(Guid deviceGuid, SetDeviceCategoriesDto dto, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        var categoryIds = await ResolveCategoryIds(dto.CategoryGuids, ct);
        await repository.ReplaceDeviceCategoriesAsync(device.Id, categoryIds, ct);

        device.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        var updated = await repository.GetDeviceByGuidAsync(device.Guid, ct)
            ?? throw new NotFoundException("Device not found after category update.");

        return DeviceView.FromEntity(updated);
    }

    /// <summary>
    /// Executes the upsert device parameter operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceParameterView result.</returns>
    public async Task<DeviceParameterView> UpsertDeviceParameter(Guid deviceGuid, UpsertDeviceParameterDto dto, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        var now = SystemClock.Instance.GetCurrentInstant();
        var key = dto.Key.Trim();
        var value = dto.Value.Trim();

        var parameter = device.Parameters.SingleOrDefault(p => p.ParamKey == key);
        if (parameter is null)
        {
            parameter = new DeviceParameter
            {
                DeviceId = device.Id,
                ParamKey = key,
                Value = value,
                CreatedAt = now,
                UpdatedAt = now
            };
            device.Parameters.Add(parameter);
        }
        else
        {
            parameter.Value = value;
            parameter.UpdatedAt = now;
        }

        device.UpdatedAt = now;

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return DeviceParameterView.FromEntity(parameter);
    }

    /// <summary>
    /// Executes the remove device parameter operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="parameterKey">Text input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RemoveDeviceParameter(Guid deviceGuid, string parameterKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parameterKey))
        {
            throw new ValidationException(
                "Validation failed.",
                new Dictionary<string, string[]>
                {
                    [nameof(parameterKey)] = ["Parameter key is required."]
                });
        }

        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException("Device not found.");

        var parameter = device.Parameters.SingleOrDefault(p => p.ParamKey == parameterKey);
        if (parameter is null)
        {
            throw new NotFoundException($"Parameter '{parameterKey}' not found on device.");
        }

        device.Parameters.Remove(parameter);
        device.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        await repository.SaveChangesAsync(ct);
    }


    /// <summary>
    /// Executes the resolve category ids operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="categoryGuids">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;long&gt; result.</returns>
    private async Task<IReadOnlyList<long>> ResolveCategoryIds(IReadOnlyCollection<Guid> categoryGuids, CancellationToken ct)
    {
        if (categoryGuids.Any(g => g == Guid.Empty))
        {
            throw new ValidationException(
                "Validation failed.",
                new Dictionary<string, string[]>
                {
                    ["CategoryGuids"] = ["Category GUID must not be empty."]
                });
        }

        var uniqueCategoryGuids = categoryGuids
            .Distinct()
            .ToArray();

        var categoryIds = await repository.GetCategoryIdsByGuidsAsync(uniqueCategoryGuids, ct);
        if (categoryIds.Count != uniqueCategoryGuids.Length)
        {
            throw new NotFoundException("One or more categories were not found.");
        }

        return categoryIds;
    }

    /// <summary>
    /// Executes the resolve maintenance status id for create operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="maintenanceStatusUuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long result.</returns>
    private async Task<long> ResolveMaintenanceStatusIdForCreate(Guid? maintenanceStatusUuid, CancellationToken ct)
    {
        if (maintenanceStatusUuid is not null)
        {
            EnsureNonEmptyGuid(maintenanceStatusUuid.Value, nameof(maintenanceStatusUuid));

            var maintenanceStatusId = await repository.TryGetMaintenanceStatusIdByGuidAsync(maintenanceStatusUuid.Value, ct);
            if (maintenanceStatusId is null)
            {
                throw new NotFoundException($"Maintenance status '{maintenanceStatusUuid}' not found.");
            }

            return maintenanceStatusId.Value;
        }

        var anyMaintenanceStatusId = await repository.TryGetAnyMaintenanceStatusIdAsync(ct);
        if (anyMaintenanceStatusId is not null)
        {
            return anyMaintenanceStatusId.Value;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var defaultStatus = new MaintenanceStatus
        {
            Uuid = Guid.CreateVersion7(),
            Name = $"Default-{Guid.CreateVersion7()}",
            Description = "Auto-created default maintenance status.",
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddMaintenanceStatusAsync(defaultStatus, ct);
        await repository.SaveChangesAsync(ct);

        return defaultStatus.Id;
    }

    /// <summary>
    /// Executes the ensure non empty guid operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="guid">Unique identifier used to target the requested entity.</param>
    /// <param name="field">Text input used by this operation.</param>
    private static void EnsureNonEmptyGuid(Guid guid, string field)
    {
        if (guid == Guid.Empty)
        {
            throw new ValidationException(
                "Validation failed.",
                new Dictionary<string, string[]>
                {
                    [field] = ["GUID must not be empty."]
                });
        }
    }
}
