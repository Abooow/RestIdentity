namespace RestIdentity.Server.Models.Ip;

public sealed class IpInfo : IIpInfo
{
    public string Ip { get; set; }
    public string Country { get; set; }
    public string Region { get; set; }
    public string City { get; set; }
    public string Longitude { get; set; }
    public string Latitude { get; set; }
    public string HostName { get; set; }
    public string Org { get; set; }
    public string PostalCode { get; set; }
    public string TimeZone { get; set; }
}
