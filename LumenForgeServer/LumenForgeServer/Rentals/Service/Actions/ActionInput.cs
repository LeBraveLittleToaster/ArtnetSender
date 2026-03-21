using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Base class for all action inputs. Carries the identity of the actor so that
/// every handler and the orchestrator can attribute the action without relying
/// on ambient HTTP context.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ActorKcId"/> is never deserialized from the request body — the
/// controller always populates it from the authenticated JWT token.
/// </para>
/// <para>
/// Concrete handlers define their own derived input classes (e.g.
/// <c>CreateRentalInput</c>) adding action-specific payload properties.
/// </para>
/// </remarks>
public abstract class ActionInput
{
    /// <summary>
    /// Keycloak subject id of the user performing the action.
    /// Always set by the controller from the JWT — never from the request body.
    /// </summary>
    [JsonIgnore]
    public string ActorKcId { get; set; } = string.Empty;
}
