using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Common.Health;

/// <summary>
/// Health endpoints used for liveness checks and role-based access validation.
/// </summary>
[ApiController]
[Route("api/v1/")]
public class HealthController
{
    /// <summary>
    /// Public health endpoint that returns a simple status payload.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Produces("application/json")]
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
    /// Health endpoint restricted to users with the REALM_USER role.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Authorize(Roles = "REALM_USER")]
    [Produces("application/json")]
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
    /// Health endpoint restricted to users with the REALM_WORKER role.
    /// </summary>
    /// <returns>A JSON payload indicating service health.</returns>
    [Authorize(Roles = "REALM_WORKER")]
    [Produces("application/json")]
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
