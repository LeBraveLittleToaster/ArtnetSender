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
/// Assignment request used for batch stock-binding creation.
/// </summary>
public sealed class StockBindingAssignment
{
    public required Guid DeviceGuid { get; init; }
    public required long ReservedAmount { get; init; }
}

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

        ValidateReservedAmount(dto.ReservedAmount);
        var (start, end) = ParseAndValidateTimeframe(dto.Start, dto.End);

        var overlappingReservedAmount = await repository.GetOverlappingReservedAmountAsync(device.Id, start, end, dto.BindingType, ct);
        if (overlappingReservedAmount + dto.ReservedAmount > device.StockAmount)
        {
            throw new ValidationException(
                $"Device '{deviceGuid}' has insufficient stock for {dto.BindingType} in the specified timeframe.",
                new Dictionary<string, string[]>());
        }

        var binding = new StockBinding
        {
            Guid = Guid.NewGuid(),
            DeviceId = device.Id,
            BindingType = dto.BindingType,
            OwnerProcessGuid = dto.OwnerProcessGuid,
            ReservedAmount = dto.ReservedAmount,
            Start = start,
            End = end,
            CreatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        await repository.AddStockBindingAsync(binding, ct);
        await repository.SaveChangesAsync(ct);

        return StockBindingView.FromEntity(binding);
    }

    /// <summary>
    /// Creates multiple stock bindings from explicit item assignments.
    /// </summary>
    /// <param name="assignments">The item assignments detailing device GUIDs and quantities.</param>
    /// <param name="dto">The binding details including type, start, and end times.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of created stock binding views.</returns>
    /// <exception cref="NotFoundException">Thrown when any device cannot be found.</exception>
    /// <exception cref="ValidationException">Thrown when binding parameters are invalid or conflict with existing bindings.</exception>
    public async Task<IReadOnlyList<StockBindingView>> CreateStockBindingsForAssignments(
        IReadOnlyCollection<StockBindingAssignment> assignments,
        CreateStockBindingDto dto,
        CancellationToken ct)
    {
        if (assignments.Count == 0)
        {
            throw new ValidationException("At least one item assignment must be provided.", new Dictionary<string, string[]>());
        }

        var (start, end) = ParseAndValidateTimeframe(dto.Start, dto.End);
        var bindings = new List<StockBinding>();
        var createdAt = SystemClock.Instance.GetCurrentInstant();
        var pendingAmountByDeviceId = new Dictionary<long, long>();

        foreach (var assignment in assignments)
        {
            ValidateReservedAmount(assignment.ReservedAmount);

            var device = await repository.GetDeviceByGuidAsync(assignment.DeviceGuid, ct)
                ?? throw new NotFoundException($"Device '{assignment.DeviceGuid}' not found.");

            var overlappingReservedAmount = await repository.GetOverlappingReservedAmountAsync(device.Id, start, end, dto.BindingType, ct);
            var pendingAmount = pendingAmountByDeviceId.TryGetValue(device.Id, out var buffered) ? buffered : 0L;
            if (overlappingReservedAmount + pendingAmount + assignment.ReservedAmount > device.StockAmount)
            {
                throw new ValidationException(
                    $"Device '{assignment.DeviceGuid}' has insufficient stock for {dto.BindingType} in the specified timeframe.",
                    new Dictionary<string, string[]>());
            }

            pendingAmountByDeviceId[device.Id] = pendingAmount + assignment.ReservedAmount;

            var binding = new StockBinding
            {
                Guid = Guid.NewGuid(),
                DeviceId = device.Id,
                BindingType = dto.BindingType,
                OwnerProcessGuid = dto.OwnerProcessGuid,
                ReservedAmount = assignment.ReservedAmount,
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
        var assignments = deviceGuids
            .Select(g => new StockBindingAssignment
            {
                DeviceGuid = g,
                ReservedAmount = dto.ReservedAmount
            })
            .ToList();

        return await CreateStockBindingsForAssignments(assignments, dto, ct);
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
    /// Retrieves stock bindings for a specific rental process owner.
    /// </summary>
    /// <param name="ownerProcessGuid">The GUID of the owner process.</param>
    /// <param name="bindingType">The type of binding to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of stock binding views for the owner process.</returns>
    public async Task<IReadOnlyList<StockBindingView>> GetStockBindingsForOwnerProcess(Guid ownerProcessGuid, BindingType bindingType, CancellationToken ct)
    {
        var bindings = await repository.GetStockBindingsByOwnerProcessGuidAsync(ownerProcessGuid, bindingType, ct);
        return bindings.Select(StockBindingView.FromEntity).ToList();
    }

    /// <summary>
    /// Checks if a device has available quantity in a timeframe.
    /// </summary>
    /// <param name="deviceGuid">The GUID of the device.</param>
    /// <param name="start">The start of the timeframe to check.</param>
    /// <param name="end">The end of the timeframe to check.</param>
    /// <param name="bindingType">The type of binding to check for conflicts.</param>
    /// <param name="amount">The amount to check for availability.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the timeframe is available, false if there are conflicts.</returns>
    /// <exception cref="NotFoundException">Thrown when the device cannot be found.</exception>
    public async Task<bool> IsTimeframeAvailable(Guid deviceGuid, string start, string end, BindingType bindingType, long amount, CancellationToken ct)
    {
        var device = await repository.GetDeviceByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");

        ValidateReservedAmount(amount);

        var (startInstant, endInstant) = ParseAndValidateTimeframe(start, end);

        var overlappingReservedAmount = await repository.GetOverlappingReservedAmountAsync(device.Id, startInstant, endInstant, bindingType, ct);
        return overlappingReservedAmount + amount <= device.StockAmount;
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
    /// Deletes a stock binding only when it belongs to the provided process owner.
    /// </summary>
    /// <param name="bindingGuid">The GUID of the binding to delete.</param>
    /// <param name="ownerProcessGuid">The GUID of the owner process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the binding cannot be found.</exception>
    /// <exception cref="ValidationException">Thrown when the binding does not belong to the specified owner.</exception>
    public async Task DeleteStockBindingForOwner(Guid bindingGuid, Guid ownerProcessGuid, CancellationToken ct)
    {
        var binding = await repository.GetStockBindingByGuidAsync(bindingGuid, ct)
            ?? throw new NotFoundException($"Stock binding '{bindingGuid}' not found.");

        if (binding.OwnerProcessGuid != ownerProcessGuid)
        {
            throw new ValidationException(
                $"Stock binding '{bindingGuid}' does not belong to process '{ownerProcessGuid}'.",
                new Dictionary<string, string[]>());
        }

        await repository.DeleteStockBindingAsync(binding, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Executes the validate reserved amount operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="amount">Numeric input used by this operation.</param>
    private static void ValidateReservedAmount(long amount)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Reserved amount must be greater than zero.", new Dictionary<string, string[]>());
        }
    }

    /// <summary>
    /// Parses and validates a timeframe, ensuring start is before end.
    /// </summary>
    /// <param name="startStr">Text input used by this operation.</param>
    /// <param name="endStr">Text input used by this operation.</param>
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
