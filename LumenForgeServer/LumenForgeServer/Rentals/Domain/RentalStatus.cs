using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// State of a rental lifecycle.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RentalStatus
{
    Requested,
    Approved,
    Rejected,
    PickedUp,
    Returned,
    Completed,
    Cancelled,
    Scrapped,
}
