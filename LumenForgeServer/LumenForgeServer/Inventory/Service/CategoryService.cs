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
    /// <summary>
    /// Executes the create category operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CategoryView result.</returns>
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

    /// <summary>
    /// Executes the get category operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="categoryGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CategoryView result.</returns>
    public async Task<CategoryView> GetCategory(Guid categoryGuid, CancellationToken ct)
    {
        var category = await repository.GetCategoryByGuidAsync(categoryGuid, ct)
            ?? throw new NotFoundException("Category not found.");

        return CategoryView.FromEntity(category);
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
    public async Task<(IReadOnlyList<CategoryView> categories, long total)> ListCategories(string? search, int limit, int offset, CancellationToken ct)
    {
        var categories = await repository.ListCategoriesAsync(search, limit, offset, ct);
        return (categories.categories.Select(CategoryView.FromEntity).ToList(), categories.total);
    }

    /// <summary>
    /// Executes the update category operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="categoryGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CategoryView result.</returns>
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

    /// <summary>
    /// Executes the delete category operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="categoryGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteCategory(Guid categoryGuid, CancellationToken ct)
    {
        var category = await repository.GetCategoryByGuidAsync(categoryGuid, ct)
            ?? throw new NotFoundException("Category not found.");

        await repository.DeleteCategoryAsync(category, ct);
        await repository.SaveChangesAsync(ct);
    }
}
