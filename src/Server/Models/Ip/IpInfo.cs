using System.Text.Json.Serialization;

namespace RestIdentity.Server.Models.Ip;

public sealed class IpInfo : IIpInfo
{
    public string Ip { get; set; }

    public string Country { get; set; }

    public string Region { get; set; }

    public string City { get; set; }

    [JsonPropertyName("loc")]
    public string Location { get; set; }

    [JsonIgnore]
    public string Latitude => Location?.Split(',').First();

    [JsonIgnore]
    public string Longitude => Location?.Split(',').Last();

    public string HostName { get; set; }

    public string Org { get; set; }

    [JsonPropertyName("postal")]
    public string PostalCode { get; set; }

    public string TimeZone { get; set; }
}
