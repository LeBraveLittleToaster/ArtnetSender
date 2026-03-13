using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Factory;
using LumenForgeServer.Inventory.Persistance;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Inventory.Service;

/// <summary>
/// Application service for category operations.
/// </summary>
public class CategoryService(IInventoryRepository repository)
{
    public async Task<CategoryView> CreateCategory(CreateCategoryDto dto, CancellationToken ct)
    {
        var category = CategoryFactory.Create(dto);

        try
        {
            await repository.AddCategoryAsync(category, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return CategoryView.FromEntity(category);
    }

    public async Task<CategoryView> GetCategory(Guid categoryGuid, CancellationToken ct)
    {
        var category = await repository.GetCategoryByGuidAsync(categoryGuid, ct)
            ?? throw new NotFoundException("Category not found.");

        return CategoryView.FromEntity(category);
    }

    public async Task<(IReadOnlyList<CategoryView> categories, long total)> ListCategories(string? search, int limit, int offset, CancellationToken ct)
    {
        var categories = await repository.ListCategoriesAsync(search, limit, offset, ct);
        return (categories.categories.Select(CategoryView.FromEntity).ToList(), categories.total);
    }

    public async Task<CategoryView> UpdateCategory(Guid categoryGuid, UpdateCategoryDto dto, CancellationToken ct)
    {
        var category = await repository.GetCategoryByGuidAsync(categoryGuid, ct)
            ?? throw new NotFoundException("Category not found.");

        if (dto.Name is not null)
        {
            category.Name = dto.Name;
        }

        if (dto.Description is not null)
        {
            category.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
        }

        category.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return CategoryView.FromEntity(category);
    }

    public async Task DeleteCategory(Guid categoryGuid, CancellationToken ct)
    {
        var category = await repository.GetCategoryByGuidAsync(categoryGuid, ct)
            ?? throw new NotFoundException("Category not found.");

        await repository.DeleteCategoryAsync(category, ct);
        await repository.SaveChangesAsync(ct);
    }
}
