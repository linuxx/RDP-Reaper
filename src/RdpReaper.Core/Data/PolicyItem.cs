using System.ComponentModel.DataAnnotations;

namespace RdpReaper.Core.Data;

public sealed class PolicyItem
{
    [Key]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
