using Newtonsoft.Json;

namespace LumenForgeServer.Auth.Dto.Views;

public record ListViewDto<T>
{
    [JsonProperty("entityList")]
    public required IReadOnlyList<T> list { get; set; }
    [JsonProperty("totalEntityCount")]
    public required long total { get; set; }
}