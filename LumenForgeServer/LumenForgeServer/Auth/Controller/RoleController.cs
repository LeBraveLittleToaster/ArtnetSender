using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Auth.Controller;

/// <summary>
/// HTTP API for listing available application roles.
/// </summary>
[Route("api/v1/auth/roles")]
[ApiController]
public class RoleController : ControllerBase
{
    /// <summary>
    /// Lists all role values defined by the application.
    /// </summary>
    /// <returns>A 200 response with the available roles.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = nameof(Permissions.RoleRead))]
    [Produces("application/json")]
    public IActionResult GetRoles()
    {
        var roles = Enum.GetValues<Permissions>()
            .Select(RoleViewDto.FromRole)
            .ToArray();

        return Ok(roles);
    }
}
