using LumenForgeServer.Common;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Persistance;
using NodaTime;
using NodaTime.Text;

namespace LumenForgeServer.Inventory.Service;

/// <summary>
/// Application service for stock binding operations.
/// </summary>
public class StockBindingService(IInventoryRepository repository)
{
    /// <summary>
    /// Creates a stock binding for a device.
    /// </summary>
    /// <param name="deviceGuid">The GUID of the device to bind.</param>
    /// <param name="dto">The binding details including type, start, and end times.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created stock binding view.</returns>
    /// <exception cref="NotFoundException">Thrown when the device cannot be found.</exception>
    /// <exception cref="ValidationException">Thrown when the binding parameters are invalid or conflict with existing bindings.</exception>
    public async Task<StockBindingView> CreateStockBinding(Guid deviceGuid, CreateStockBindingDto dto, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

        var (start, end) = ParseAndValidateTimeframe(dto.Start, dto.End);

        // Check for conflicting bindings
        var hasConflict = await repository.HasConflictingBindingsAsync(device.Id, start, end, dto.BindingType, ct);
        if (hasConflict)
        {
            throw new ValidationException($"Device has conflicting {dto.BindingType} bindings during the specified timeframe.", new Dictionary<string, string[]>());
        }

        var binding = new StockBinding
        {
            Guid = Guid.NewGuid(),
            DeviceId = device.Id,
            BindingType = dto.BindingType,
            Start = start,
            End = end,
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        await repository.AddStockBindingAsync(binding, ct);
        await repository.SaveChangesAsync(ct);

        return StockBindingView.FromEntity(binding);
    }

    /// <summary>
    /// Creates multiple stock bindings for different devices with the same timeframe.
    /// </summary>
    /// <param name="deviceGuids">Collection of device GUIDs to bind.</param>
    /// <param name="dto">The binding details including type, start, and end times.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of created stock binding views.</returns>
    /// <exception cref="NotFoundException">Thrown when any device cannot be found.</exception>
    /// <exception cref="ValidationException">Thrown when binding parameters are invalid or conflict with existing bindings.</exception>
    public async Task<IReadOnlyList<StockBindingView>> CreateStockBindingsForMultipleDevices(
        IReadOnlyCollection<Guid> deviceGuids,
        CreateStockBindingDto dto,
        CancellationToken ct)
    {
        if (deviceGuids.Count == 0)
        {
            throw new ValidationException("At least one device GUID must be provided.", new Dictionary<string, string[]>());
        }

        var (start, end) = ParseAndValidateTimeframe(dto.Start, dto.End);
        var bindings = new List<StockBinding>();
        var createdAt = SystemClock.Instance.GetCurrentInstant();

        foreach (var deviceGuid in deviceGuids)
        {
            var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
                ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

            // Check for conflicting bindings
            var hasConflict = await repository.HasConflictingBindingsAsync(device.Id, start, end, dto.BindingType, ct);
            if (hasConflict)
            {
                throw new ValidationException($"Device '{deviceGuid}' has conflicting {dto.BindingType} bindings during the specified timeframe.", new Dictionary<string, string[]>());
            }

            var binding = new StockBinding
            {
                Guid = Guid.NewGuid(),
                DeviceId = device.Id,
                BindingType = dto.BindingType,
                Start = start,
                End = end,
                CreatedAt = createdAt
            };

            bindings.Add(binding);
        }

        await repository.AddStockBindingsAsync(bindings, ct);
        await repository.SaveChangesAsync(ct);

        return bindings.Select(StockBindingView.FromEntity).ToList();
    }

    /// <summary>
    /// Retrieves all stock bindings for a device.
    /// </summary>
    /// <param name="deviceGuid">The GUID of the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of stock binding views for the device.</returns>
    /// <exception cref="NotFoundException">Thrown when the device cannot be found.</exception>
    public async Task<IReadOnlyList<StockBindingView>> GetStockBindingsForDevice(Guid deviceGuid, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

        var bindings = await repository.GetStockBindingsByDeviceIdAsync(device.Id, ct);
        return bindings.Select(StockBindingView.FromEntity).ToList();
    }

    /// <summary>
    /// Checks if a device has available timeframes that don't conflict with existing bindings.
    /// </summary>
    /// <param name="deviceGuid">The GUID of the device.</param>
    /// <param name="start">The start of the timeframe to check.</param>
    /// <param name="end">The end of the timeframe to check.</param>
    /// <param name="bindingType">The type of binding to check for conflicts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the timeframe is available, false if there are conflicts.</returns>
    /// <exception cref="NotFoundException">Thrown when the device cannot be found.</exception>
    public async Task<bool> IsTimeframeAvailable(Guid deviceGuid, string start, string end, BindingType bindingType, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

        var (startInstant, endInstant) = ParseAndValidateTimeframe(start, end);

        var hasConflict = await repository.HasConflictingBindingsAsync(device.Id, startInstant, endInstant, bindingType, ct);
        return !hasConflict;
    }

    /// <summary>
    /// Deletes a stock binding.
    /// </summary>
    /// <param name="bindingGuid">The GUID of the binding to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the binding cannot be found.</exception>
    public async Task DeleteStockBinding(Guid bindingGuid, CancellationToken ct)
    {
        var binding = await repository.GetStockBindingByGuidAsync(bindingGuid, ct)
            ?? throw new NotFoundException($"Stock binding '{bindingGuid}' not found.");

        await repository.DeleteStockBindingAsync(binding, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Parses and validates a timeframe, ensuring start is before end.
    /// </summary>
    /// <param name="startStr">ISO-8601 formatted start time string.</param>
    /// <param name="endStr">ISO-8601 formatted end time string.</param>
    /// <returns>Tuple of parsed Instant values.</returns>
    /// <exception cref="ValidationException">Thrown when parsing fails or start is not before end.</exception>
    private static (Instant start, Instant end) ParseAndValidateTimeframe(string startStr, string endStr)
    {
        if (string.IsNullOrWhiteSpace(startStr))
        {
            throw new ValidationException("Start time cannot be empty.", new Dictionary<string, string[]>());
        }

        if (string.IsNullOrWhiteSpace(endStr))
        {
            throw new ValidationException("End time cannot be empty.", new Dictionary<string, string[]>());
        }

        var startResult = InstantPattern.ExtendedIso.Parse(startStr);
        if (!startResult.Success)
        {
            throw new ValidationException($"Invalid start time format: {startResult.Exception?.Message}", new Dictionary<string, string[]>());
        }

        var endResult = InstantPattern.ExtendedIso.Parse(endStr);
        if (!endResult.Success)
        {
            throw new ValidationException($"Invalid end time format: {endResult.Exception?.Message}", new Dictionary<string, string[]>());
        }

        var start = startResult.Value;
        var end = endResult.Value;

        if (start >= end)
        {
            throw new ValidationException("Start time must be before end time.", new Dictionary<string, string[]>());
        }

        return (start, end);
    }
}
