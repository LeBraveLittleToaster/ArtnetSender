using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Query;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Inventory.Controller;

/// <summary>
/// HTTP API for managing inventory categories.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/inventory/categories</c>.
/// </remarks>
[Route("api/v1/inventory/categories")]
[ApiController]
[Tags("Inventory – Categories")]
public class CategoryController(CategoryService categoryService) : ControllerBase
{
    /// <summary>
    /// Lists categories with optional paging and search.
    /// </summary>
    /// <remarks>
    /// Example query: <c>GET /api/v1/inventory/categories?search=camera&amp;limit=50&amp;offset=0</c>
    /// </remarks>
    /// <param name="query">Paging and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with category results.</returns>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.CategoryRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListCategories([FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var categories = await categoryService.ListCategories(query.Search, query.Limit, query.Offset, ct);
        return Ok(new ListViewDto<CategoryView> { list = categories.categories, total = categories.total });
    }

    /// <summary>
    /// Retrieves a single category by its GUID.
    /// </summary>
    /// <param name="categoryGuid">Unique category identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the category payload.</returns>
    [HttpGet("{categoryGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetCategory([FromRoute] Guid categoryGuid, CancellationToken ct)
    {
        var category = await categoryService.GetCategory(categoryGuid, ct);
        return Ok(category);
    }

    /// <summary>
    /// Creates a new inventory category.
    /// </summary>
    /// <param name="dto">Category creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created category.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.CategoryCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var category = await categoryService.CreateCategory(dto, ct);
        return CreatedAtAction(nameof(GetCategory), new { categoryGuid = category.Guid }, category);
    }

    /// <summary>
    /// Partially updates an existing category. At least one of name or description must be provided.
    /// </summary>
    /// <param name="categoryGuid">Category to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated category.</returns>
    [HttpPatch("{categoryGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid categoryGuid, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
    {
        var category = await categoryService.UpdateCategory(categoryGuid, dto, ct);
        return Ok(category);
    }

    /// <summary>
    /// Permanently deletes a category. Devices in this category are unlinked, not deleted.
    /// </summary>
    /// <param name="categoryGuid">Category to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
    [HttpDelete("{categoryGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid categoryGuid, CancellationToken ct)
    {
        await categoryService.DeleteCategory(categoryGuid, ct);
        return NoContent();
    }
}
