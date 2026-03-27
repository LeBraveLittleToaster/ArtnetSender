using System.Text.Json.Serialization;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.Command;

public class CreateRentalFormInput
{
    [JsonPropertyName("event_name")]
    public required string EventName {get;set;}
    [JsonPropertyName("event_description")]
    public required string EventDescription {get;set;}
    [JsonPropertyName("event_start_date")]
    public required Instant EventStartDate {get;set;}
    [JsonPropertyName("event_end_date")]
    public required Instant EventEndDate {get;set;}
    [JsonPropertyName("event_location")]
    public required string EventLocation {get;set;}
}