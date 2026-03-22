using Newtonsoft.Json;

namespace LumenForgeServer.Auth.Dto.Views;

/// <summary>
/// Generic paginated list wrapper used across list endpoints.
/// </summary>
/// <typeparam name="T">Type of the items in the list.</typeparam>
public record ListViewDto<T>
{
    /// <summary>Page of result items.</summary>
    [JsonProperty("entityList")]
    public required IReadOnlyList<T> list { get; set; }
    /// <summary>Total number of matching items across all pages.</summary>
    [JsonProperty("totalEntityCount")]
    public required long total { get; set; }
}