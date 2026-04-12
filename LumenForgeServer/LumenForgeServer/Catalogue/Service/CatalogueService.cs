using LumenForgeServer.Catalogue.Domain;
using LumenForgeServer.Catalogue.Dto.Command;
using LumenForgeServer.Catalogue.Dto.View;
using LumenForgeServer.Catalogue.Persistence;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Persistance;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Catalogue.Service;

/// <summary>
/// Application service for catalogue operations.
/// </summary>
public class CatalogueService(
    ICatalogueRepository repository,
    IInventoryRepository inventoryRepository)
{
    /// <summary>
    /// Executes the create item operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CatalogueItemView result.</returns>
    public async Task<CatalogueItemView> CreateItem(CreateCatalogueItemDto dto, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceId(dto.DeviceGuid, ct);
        var now = SystemClock.Instance.GetCurrentInstant();

        var item = new CatalogueItem
        {
            Guid = Guid.NewGuid(),
            DeviceId = deviceId,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim(),
            IsPublished = dto.IsPublished,
            SortOrder = dto.SortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

        try
        {
            await repository.AddItemAsync(item, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        var persisted = await repository.GetItemByGuidAsync(item.Guid, true, ct)
            ?? throw new NotFoundException("Catalogue item not found after creation.");

        return CatalogueItemView.FromEntity(persisted);
    }

    /// <summary>
    /// Executes the get item operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="itemGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="includeUnpublished">Boolean flag controlling the operation behavior.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CatalogueItemView result.</returns>
    public async Task<CatalogueItemView> GetItem(Guid itemGuid, bool includeUnpublished, CancellationToken ct)
    {
        var item = await repository.GetItemByGuidAsync(itemGuid, includeUnpublished, ct)
            ?? throw new NotFoundException("Catalogue item not found.");

        return CatalogueItemView.FromEntity(item);
    }

    /// <summary>
    /// Executes the task operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="publishedOnly">Boolean flag controlling the operation behavior.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<CatalogueItemView> items, long total)> ListItems(string? search, int limit, int offset, bool publishedOnly, CancellationToken ct)
    {
        var items = await repository.ListItemsAsync(search, limit, offset, publishedOnly, ct);
        return (items.items.Select(CatalogueItemView.FromEntity).ToList(), items.total);
    }

    /// <summary>
    /// Executes the update item operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="itemGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CatalogueItemView result.</returns>
    public async Task<CatalogueItemView> UpdateItem(Guid itemGuid, UpdateCatalogueItemDto dto, CancellationToken ct)
    {
        var item = await repository.GetItemByGuidAsync(itemGuid, true, ct)
            ?? throw new NotFoundException("Catalogue item not found.");

        if (dto.DeviceGuid is not null)
        {
            item.DeviceId = await ResolveDeviceId(dto.DeviceGuid.Value, ct);
        }

        if (dto.Name is not null)
        {
            item.Name = dto.Name.Trim();
        }

        if (dto.Description is not null)
        {
            item.Description = dto.Description.Trim();
        }

        if (dto.PhotoUrl is not null)
        {
            item.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim();
        }

        if (dto.IsPublished.HasValue)
        {
            item.IsPublished = dto.IsPublished.Value;
        }

        if (dto.SortOrder.HasValue)
        {
            item.SortOrder = dto.SortOrder.Value;
        }

        item.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        var updated = await repository.GetItemByGuidAsync(itemGuid, true, ct)
            ?? throw new NotFoundException("Catalogue item not found after update.");

        return CatalogueItemView.FromEntity(updated);
    }

    /// <summary>
    /// Executes the delete item operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="itemGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteItem(Guid itemGuid, CancellationToken ct)
    {
        var item = await repository.GetItemByGuidAsync(itemGuid, true, ct)
            ?? throw new NotFoundException("Catalogue item not found.");

        await repository.DeleteItemAsync(item, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Executes the resolve device id operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long result.</returns>
    private async Task<long> ResolveDeviceId(Guid deviceGuid, CancellationToken ct)
    {
        if (deviceGuid == Guid.Empty)
        {
            throw new ValidationException(
                "Validation failed.",
                new Dictionary<string, string[]>
                {
                    [nameof(deviceGuid)] = ["Device GUID must not be empty."]
                });
        }

        return await inventoryRepository.TryGetDeviceIdByGuidAsync(deviceGuid, ct)
            ?? throw new NotFoundException($"Device '{deviceGuid}' not found.");
    }
}