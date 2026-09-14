using System.Text.Json.Serialization;

namespace ElectronicLive.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventProvider
{
    Ticketmaster,
    Skiddle,
}
