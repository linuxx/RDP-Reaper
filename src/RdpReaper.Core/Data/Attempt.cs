using System;
using System.ComponentModel.DataAnnotations;

namespace RdpReaper.Core.Data;

public sealed class Attempt
{
    [Key]
    public long AttemptId { get; set; }

    public DateTimeOffset Time { get; set; }
    [MaxLength(64)]
    public string Ip { get; set; } = string.Empty;
    [MaxLength(64)]
    public string Subnet { get; set; } = string.Empty;
    [MaxLength(256)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(32)]
    public string Outcome { get; set; } = string.Empty;
    public int LogonType { get; set; }
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;
    public int EventId { get; set; }
}
