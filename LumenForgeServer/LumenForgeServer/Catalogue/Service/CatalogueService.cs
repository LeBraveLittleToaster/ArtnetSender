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
public class CatalogueService(ICatalogueRepository repository, IInventoryRepository inventoryRepository)
{
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

    public async Task<CatalogueItemView> GetItem(Guid itemGuid, bool includeUnpublished, CancellationToken ct)
    {
        var item = await repository.GetItemByGuidAsync(itemGuid, includeUnpublished, ct)
            ?? throw new NotFoundException("Catalogue item not found.");

        return CatalogueItemView.FromEntity(item);
    }

    public async Task<(IReadOnlyList<CatalogueItemView> items, long total)> ListItems(string? search, int limit, int offset, bool publishedOnly, CancellationToken ct)
    {
        var items = await repository.ListItemsAsync(search, limit, offset, publishedOnly, ct);
        return (items.items.Select(CatalogueItemView.FromEntity).ToList(), items.total);
    }

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

    public async Task DeleteItem(Guid itemGuid, CancellationToken ct)
    {
        var item = await repository.GetItemByGuidAsync(itemGuid, true, ct)
            ?? throw new NotFoundException("Catalogue item not found.");

        await repository.DeleteItemAsync(item, ct);
        await repository.SaveChangesAsync(ct);
    }

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