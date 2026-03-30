using LumenForgeServer.Auth.Domain;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Views;

/// <summary>
/// Represents an application role for API responses.
/// </summary>
public sealed record RoleViewDto
{
    /// <summary>Display name of the permission (e.g. "DeviceRead").</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    /// <summary>Numeric value of the permission enum.</summary>
    [JsonPropertyName("value")]
    public required int Value { get; init; }

    /// <summary>
    /// Executes the from role operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="permissions">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static RoleViewDto FromRole(Permissions permissions)
    {
        return new RoleViewDto
        {
            Name = permissions.ToString(),
            Value = (int)permissions
        };
    }
}
