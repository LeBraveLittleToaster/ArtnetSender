using LumenForgeServer.Auth.Domain;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Views;

/// <summary>
/// Represents an application role for API responses.
/// </summary>
public sealed record RoleViewDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("value")]
    public required int Value { get; init; }

    public static RoleViewDto FromRole(Permissions permissions)
    {
        return new RoleViewDto
        {
            Name = permissions.ToString(),
            Value = (int)permissions
        };
    }
}
