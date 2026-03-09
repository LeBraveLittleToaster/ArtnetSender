using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Query;
using LumenForgeServer.Inventory.Dto.Update;
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
        return Ok(categories);
    }

    [HttpGet("{categoryGuid:guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetCategory([FromRoute] Guid categoryGuid, CancellationToken ct)
    {
        var category = await categoryService.GetCategory(categoryGuid, ct);
        return Ok(category);
    }

    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.CategoryCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var category = await categoryService.CreateCategory(dto, ct);
        return CreatedAtAction(nameof(GetCategory), new { categoryGuid = category.Guid }, category);
    }

    [HttpPatch("{categoryGuid:guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid categoryGuid, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
    {
        var category = await categoryService.UpdateCategory(categoryGuid, dto, ct);
        return Ok(category);
    }

    [HttpDelete("{categoryGuid:guid}")]
    [Authorize(Roles = nameof(Permissions.CategoryDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid categoryGuid, CancellationToken ct)
    {
        await categoryService.DeleteCategory(categoryGuid, ct);
        return NoContent();
    }
}
