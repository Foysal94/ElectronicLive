using System.Text.Json.Serialization;

namespace ElectronicLive.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventStatus
{
    Unknown,
    OnSale,
    SoldOut,
    Cancelled,
    Postponed,
}
