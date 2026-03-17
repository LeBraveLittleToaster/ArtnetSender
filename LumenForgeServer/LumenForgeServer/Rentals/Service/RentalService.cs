using LumenForgeServer.Common;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Persistance;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;
using NodaTime.Text;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Application service for rental lifecycle operations and stock-binding conflict checks.
/// </summary>
public class RentalService(IRentalRepository repository, IInventoryRepository inventoryRepository)
{
    public async Task<RentalView> CreateRental(CreateRentalDto dto, string customerUserId, CancellationToken ct)
    {
        var rentalStatusId = await repository.TryGetRentalStatusIdByGuidAsync(dto.RentalStatusGuid, ct)
            ?? throw new NotFoundException($"Rental status '{dto.RentalStatusGuid}' not found.");

        var plannedPickupAt = ParseOptionalInstant(dto.PlannedPickupAt, "planned_pickup_at");
        var plannedReturnAt = ParseOptionalInstant(dto.PlannedReturnAt, "planned_return_at");

        if (plannedPickupAt.HasValue && plannedReturnAt.HasValue && plannedPickupAt >= plannedReturnAt)
        {
            throw new ValidationException("Planned pickup must be before planned return.", new Dictionary<string, string[]>
            {
                ["planned_pickup_at"] = ["Must be earlier than planned_return_at."]
            });
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var rental = new Rental
        {
            Uuid = Guid.NewGuid(),
            RentalStatusId = rentalStatusId,
            CustomerUserId = customerUserId,
            Request = new RentalRequest
            {
                Title = dto.RequestTitle?.Trim(),
                Description = dto.RequestDescription?.Trim(),
                EventName = dto.EventName?.Trim(),
                CustomerNotes = dto.CustomerNotes?.Trim(),
                DeliveryAddress = dto.DeliveryAddress?.Trim(),
                Priority = dto.Priority,
            },
            Schedule = new RentalSchedule
            {
                RequestedAt = now,
                PlannedPickupAt = plannedPickupAt,
                PlannedReturnAt = plannedReturnAt,
            },
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddRentalAsync(rental, ct);
        await repository.SaveChangesAsync(ct);

        var persisted = await repository.GetRentalByGuidAsync(rental.Uuid, RentalInclude.Items, ct)
            ?? throw new NotFoundException("Rental not found after creation.");

        return RentalView.FromEntity(persisted);
    }

    public Task<RentalView> GetRental(Guid rentalGuid, CancellationToken ct)
        => GetRental(rentalGuid, RentalInclude.None, ct);

    public async Task<RentalView> GetRental(Guid rentalGuid, RentalInclude include, CancellationToken ct)
    {
        var rental = await repository.GetRentalByGuidAsync(rentalGuid, include, ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        return RentalView.FromEntity(rental);
    }

    public Task<(IReadOnlyList<RentalView> items, long total)> ListRentals(RentalQueryDto query, CancellationToken ct)
        => ListRentals(query, RentalInclude.None, ct);

    public async Task<(IReadOnlyList<RentalView> items, long total)> ListRentals(
        RentalQueryDto query,
        RentalInclude include,
        CancellationToken ct)
    {
        var (items, total) = await repository.ListRentalsAsync(
            query.Search,
            query.CustomerUserId,
            query.Priority,
            query.Limit,
            query.Offset,
            include,
            ct);

        return (items.Select(RentalView.FromEntity).ToList(), total);
    }

    public async Task<RentalView> UpdateRental(Guid rentalGuid, UpdateRentalDto dto, CancellationToken ct)
    {
        var rental = await repository.GetRentalByGuidAsync(rentalGuid, RentalInclude.None, ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        if (dto.RentalStatusGuid.HasValue)
        {
            rental.RentalStatusId = await repository.TryGetRentalStatusIdByGuidAsync(dto.RentalStatusGuid.Value, ct)
                ?? throw new NotFoundException($"Rental status '{dto.RentalStatusGuid}' not found.");
        }

        if (dto.RequestTitle is not null) rental.Request.Title = dto.RequestTitle.Trim();
        if (dto.RequestDescription is not null) rental.Request.Description = dto.RequestDescription.Trim();
        if (dto.EventName is not null) rental.Request.EventName = dto.EventName.Trim();
        if (dto.CustomerNotes is not null) rental.Request.CustomerNotes = dto.CustomerNotes.Trim();
        if (dto.DeliveryAddress is not null) rental.Request.DeliveryAddress = dto.DeliveryAddress.Trim();
        if (dto.Priority.HasValue) rental.Request.Priority = dto.Priority.Value;

        if (dto.PlannedPickupAt is not null)
            rental.Schedule.PlannedPickupAt = ParseOptionalInstant(dto.PlannedPickupAt, "planned_pickup_at");

        if (dto.PlannedReturnAt is not null)
            rental.Schedule.PlannedReturnAt = ParseOptionalInstant(dto.PlannedReturnAt, "planned_return_at");

        if (rental.Schedule.PlannedPickupAt.HasValue && rental.Schedule.PlannedReturnAt.HasValue
            && rental.Schedule.PlannedPickupAt >= rental.Schedule.PlannedReturnAt)
        {
            throw new ValidationException("Planned pickup must be before planned return.", new Dictionary<string, string[]>
            {
                ["planned_pickup_at"] = ["Must be earlier than planned_return_at."]
            });
        }

        rental.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        await repository.SaveChangesAsync(ct);

        var updated = await repository.GetRentalByGuidAsync(rentalGuid, RentalInclude.Items, ct)
            ?? throw new NotFoundException("Rental not found after update.");

        return RentalView.FromEntity(updated);
    }

    public async Task DeleteRental(Guid rentalGuid, CancellationToken ct)
    {
        var rental = await repository.GetRentalByGuidAsync(rentalGuid, RentalInclude.None, ct)
            ?? throw new NotFoundException($"Rental '{rentalGuid}' not found.");

        await repository.DeleteRentalAsync(rental, ct);
        await repository.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<StockBindingConflictView> items, long total)> ListConflicts(
        RentalConflictQueryDto query,
        CancellationToken ct)
    {
        var deviceId = await inventoryRepository.TryGetDeviceIdByGuidAsync(query.DeviceGuid, ct)
            ?? throw new NotFoundException($"Device '{query.DeviceGuid}' not found.");

        var (start, end) = ParseAndValidateTimeframe(query.Start, query.End);

        var (bindings, total) = await repository.ListConflictingBindingsAsync(
            deviceId, start, end, query.BindingType, query.Limit, query.Offset, ct);

        return (bindings.Select(StockBindingConflictView.FromEntity).ToList(), total);
    }

    private static Instant ParseRequiredInstant(string value, string fieldName)
    {
        var result = InstantPattern.ExtendedIso.Parse(value);
        if (!result.Success)
        {
            throw new ValidationException(
                $"Invalid {fieldName} format: {result.Exception?.Message}",
                new Dictionary<string, string[]>
                {
                    [fieldName] = [$"Invalid ISO-8601 instant: {result.Exception?.Message}"]
                });
        }

        return result.Value;
    }

    private static Instant? ParseOptionalInstant(string? value, string fieldName)
    {
        if (value is null) return null;
        return ParseRequiredInstant(value, fieldName);
    }

    private static (Instant start, Instant end) ParseAndValidateTimeframe(string startStr, string endStr)
    {
        var start = ParseRequiredInstant(startStr, "start");
        var end = ParseRequiredInstant(endStr, "end");

        if (start >= end)
        {
            throw new ValidationException("Start time must be before end time.", new Dictionary<string, string[]>
            {
                ["start"] = ["Must be earlier than end."]
            });
        }

        return (start, end);
    }
}
