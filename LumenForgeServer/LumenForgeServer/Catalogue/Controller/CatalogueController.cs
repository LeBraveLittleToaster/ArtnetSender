using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Catalogue.Dto.Command;
using LumenForgeServer.Catalogue.Dto.Query;
using LumenForgeServer.Catalogue.Dto.View;
using LumenForgeServer.Catalogue.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Catalogue.Controller;

/// <summary>
/// HTTP API for public catalogue items.
/// </summary>
/// <remarks>
/// Catalogue items wrap inventory devices for customer-facing display.
/// List and detail endpoints require authentication; mutation endpoints require CatalogueCreate/Update/Delete.
/// </remarks>
[Route("api/v1/catalogue/items")]
[ApiController]
[Tags("Catalogue")]
public class CatalogueController(CatalogueService catalogueService) : ControllerBase
{
    /// <summary>
    /// Lists catalogue items with optional paging, search, and published-only filtering.
    /// </summary>
    /// <remarks>
    /// By default only published items are returned. Pass <c>published_only=false</c>
    /// to include unpublished items (requires CatalogueRead permission).
    /// </remarks>
    /// <param name="query">Paging, search, and filter parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with catalogue item results.</returns>
    [HttpGet("")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListItems([FromQuery] CatalogueQueryDto query, CancellationToken ct)
    {

        var items = await catalogueService.ListItems(query.Search, query.Limit, query.Offset, query.PublishedOnly, ct);
        return Ok(new ListViewDto<CatalogueItemView> { list = items.items, total = items.total });
    }

    /// <summary>
    /// Retrieves a single catalogue item by GUID.
    /// </summary>
    /// <remarks>
    /// Users with CatalogueRead permission can see unpublished items;
    /// regular users will receive 404 for unpublished items.
    /// </remarks>
    /// <param name="itemGuid">Unique catalogue item identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the catalogue item payload.</returns>
    [HttpGet("{itemGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetItem([FromRoute] Guid itemGuid, CancellationToken ct)
    {
        var includeUnpublished = User.IsInRole(nameof(Permissions.CatalogueRead));
        var item = await catalogueService.GetItem(itemGuid, includeUnpublished, ct);
        return Ok(item);
    }

    /// <summary>
    /// Creates a new catalogue item linked to an existing device.
    /// </summary>
    /// <remarks>
    /// The referenced device must exist. A device can only have one catalogue item;
    /// duplicates will result in a 409 Conflict.
    /// </remarks>
    /// <param name="dto">Catalogue item creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created catalogue item.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.CatalogueCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateItem([FromBody] CreateCatalogueItemDto dto, CancellationToken ct)
    {
        var item = await catalogueService.CreateItem(dto, ct);
        return CreatedAtAction(nameof(GetItem), new { itemGuid = item.Guid, include_unpublished = true }, item);
    }

    /// <summary>
    /// Partially updates a catalogue item.
    /// </summary>
    /// <param name="itemGuid">Item to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated catalogue item.</returns>
    [HttpPatch("{itemGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CatalogueUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateItem([FromRoute] Guid itemGuid, [FromBody] UpdateCatalogueItemDto dto, CancellationToken ct)
    {
        var item = await catalogueService.UpdateItem(itemGuid, dto, ct);
        return Ok(item);
    }

    /// <summary>
    /// Permanently deletes a catalogue item. The underlying device is not affected.
    /// </summary>
    /// <param name="itemGuid">Item to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
    [HttpDelete("{itemGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CatalogueDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem([FromRoute] Guid itemGuid, CancellationToken ct)
    {
        await catalogueService.DeleteItem(itemGuid, ct);
        return NoContent();
    }
}