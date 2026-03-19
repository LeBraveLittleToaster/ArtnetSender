using Newtonsoft.Json.Converters;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Common;

/// <summary>
/// Helper for creating shared JSON serializer options used across the application.
/// </summary>
public static class Json
{
    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return options;
    }

}
