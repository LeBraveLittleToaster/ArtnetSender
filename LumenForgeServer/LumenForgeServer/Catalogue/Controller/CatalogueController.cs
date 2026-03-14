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
[Route("api/v1/catalogue/items")]
[ApiController]
public class CatalogueController(CatalogueService catalogueService) : ControllerBase
{
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Produces("application/json")]
    public async Task<IActionResult> ListItems([FromQuery] CatalogueQueryDto query, CancellationToken ct)
    {
        if (!query.PublishedOnly && !CanReadUnpublished())
        {
            return ChallengeOrForbid();
        }

        var items = await catalogueService.ListItems(query.Search, query.Limit, query.Offset, query.PublishedOnly, ct);
        return Ok(new ListViewDto<CatalogueItemView> { list = items.items, total = items.total });
    }

    [HttpGet("{itemGuid:Guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetItem([FromRoute] Guid itemGuid, [FromQuery(Name = "include_unpublished")] bool includeUnpublished, CancellationToken ct)
    {
        if (includeUnpublished && !CanReadUnpublished())
        {
            return ChallengeOrForbid();
        }

        var item = await catalogueService.GetItem(itemGuid, includeUnpublished, ct);
        return Ok(item);
    }

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

    [HttpPatch("{itemGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CatalogueUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateItem([FromRoute] Guid itemGuid, [FromBody] UpdateCatalogueItemDto dto, CancellationToken ct)
    {
        var item = await catalogueService.UpdateItem(itemGuid, dto, ct);
        return Ok(item);
    }

    [HttpDelete("{itemGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.CatalogueDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteItem([FromRoute] Guid itemGuid, CancellationToken ct)
    {
        await catalogueService.DeleteItem(itemGuid, ct);
        return NoContent();
    }

    private bool CanReadUnpublished()
        => User.Identity?.IsAuthenticated == true && User.IsInRole(nameof(Permissions.CatalogueRead));

    private IActionResult ChallengeOrForbid()
        => User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized();
}