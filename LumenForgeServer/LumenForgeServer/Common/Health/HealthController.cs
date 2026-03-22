using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Common.Health;

/// <summary>
/// Health endpoints used for liveness checks and role-based access validation.
/// </summary>
[ApiController]
[Route("api/v1/")]
[Tags("Health")]
public class HealthController
{
    /// <summary>
    /// Public liveness probe — returns a status payload without authentication.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [HttpGet("health")]
    public ActionResult<IEnumerable<string>> GetHealth()
    {
        var statusAggregate = new Dictionary<string, string>
        {
            ["status"] = "healthy",
        };
        return new JsonResult(statusAggregate);
    }

    /// <summary>
    /// Authenticated health check — requires the REALM_USER Keycloak role.
    /// Use this to verify that the JWT pipeline is working for regular users.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Authorize(Roles = "REALM_USER")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpGet("health2")]
    public ActionResult<IEnumerable<string>> GetHealth2()
    {
        var statusAggregate = new Dictionary<string, string>
        {
            ["status"] = "healthy",
        };
        return new JsonResult(statusAggregate);
    }

    /// <summary>
    /// Authenticated health check — requires the REALM_WORKER Keycloak role.
    /// Use this to verify that the JWT pipeline is working for worker-level accounts.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Authorize(Roles = "REALM_WORKER")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpGet("health3")]
    public ActionResult<IEnumerable<string>> GetHealth3()
    {
        var statusAggregate = new Dictionary<string, string>
        {
            ["status"] = "healthy",
        };
        return new JsonResult(statusAggregate);
    }
}
