namespace RestIdentity.Server.Models.Ip;

public interface IIpInfo
{
    string Ip { get; }
    string Country { get; }
    string Region { get; }
    string City { get; }
    string Longitude { get; }
    string Latitude { get; }
    string HostName { get; }
    string Org { get; }
    string PostalCode { get; }
    string TimeZone { get; }
}
