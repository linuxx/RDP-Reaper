using System;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class BanView
{
    public BanView(ApiClient.BanRecord record)
    {
        BanType = record.BanType ?? string.Empty;
        Key = record.Key ?? string.Empty;
        CreatedAtUtc = record.CreatedAt?.ToString("u") ?? "-";
        ExpiresAtUtc = record.ExpiresAt?.ToString("u") ?? "-";
        Reason = record.Reason ?? string.Empty;
        Permanent = record.Permanent;
    }

    public string BanType { get; }
    public string Key { get; }
    public string CreatedAtUtc { get; }
    public string ExpiresAtUtc { get; }
    public string Reason { get; }
    public bool Permanent { get; }
}
