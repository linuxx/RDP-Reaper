using System;
using System.ComponentModel.DataAnnotations;

namespace RdpReaper.Core.Data;

public sealed class AuditLog
{
    [Key]
    public long ActionId { get; set; }

    public DateTimeOffset Time { get; set; }
    [MaxLength(128)]
    public string Actor { get; set; } = string.Empty;
    [MaxLength(64)]
    public string ActionType { get; set; } = string.Empty;
    [MaxLength(256)]
    public string Target { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
