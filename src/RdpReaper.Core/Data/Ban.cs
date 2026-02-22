using System;
using System.ComponentModel.DataAnnotations;

namespace RdpReaper.Core.Data;

public sealed class Ban
{
    [Key]
    public long BanId { get; set; }

    [MaxLength(32)]
    public string BanType { get; set; } = string.Empty;
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool Permanent { get; set; }
    [MaxLength(256)]
    public string Reason { get; set; } = string.Empty;
    [MaxLength(64)]
    public string SourcePolicy { get; set; } = string.Empty;
    public DateTimeOffset? LastSeenAt { get; set; }
}
