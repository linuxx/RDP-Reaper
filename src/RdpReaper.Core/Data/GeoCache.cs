using System;
using System.ComponentModel.DataAnnotations;

namespace RdpReaper.Core.Data;

public sealed class GeoCache
{
    [Key]
    [MaxLength(64)]
    public string Ip { get; set; } = string.Empty;

    [MaxLength(8)]
    public string CountryCode { get; set; } = string.Empty;
    [MaxLength(128)]
    public string Country { get; set; } = string.Empty;
    [MaxLength(128)]
    public string Region { get; set; } = string.Empty;
    [MaxLength(128)]
    public string City { get; set; } = string.Empty;
    [MaxLength(64)]
    public string Asn { get; set; } = string.Empty;
    [MaxLength(256)]
    public string Isp { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public int FailureCount { get; set; }
    public bool IsPartial { get; set; }
}
