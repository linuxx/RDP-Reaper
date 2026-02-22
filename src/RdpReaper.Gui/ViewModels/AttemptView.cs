using System;
using System.Text.Json;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class AttemptView
{
    private readonly string _searchText;
    private readonly string _json;

    public AttemptView(ApiClient.AttemptRecord record)
    {
        TimeUtc = record.Time?.ToString("u") ?? "-";
        Ip = record.Ip ?? string.Empty;
        Subnet = record.Subnet ?? string.Empty;
        Username = record.Username ?? string.Empty;
        Outcome = record.Outcome ?? string.Empty;
        Status = record.Status ?? string.Empty;
        LogonType = record.LogonType.ToString();
        EventId = record.EventId.ToString();

        _searchText = string.Join(' ', new[]
        {
            Ip,
            Subnet,
            Username,
            Outcome,
            Status,
            LogonType,
            EventId
        });

        _json = JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    public string TimeUtc { get; }
    public string Ip { get; }
    public string Subnet { get; }
    public string Username { get; }
    public string Outcome { get; }
    public string Status { get; }
    public string LogonType { get; }
    public string EventId { get; }
    public string Json => _json;

    public bool Matches(string query)
    {
        return _searchText.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
