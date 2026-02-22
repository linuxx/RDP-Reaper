using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class LogEntryView
{
    public LogEntryView(ApiClient.LogEntry entry)
    {
        TimeUtc = entry.TimeUtc?.ToString("u") ?? "-";
        Level = entry.Level ?? string.Empty;
        Source = entry.Source ?? string.Empty;
        EventId = entry.EventId.ToString();
        Message = entry.Message ?? string.Empty;
    }

    public string TimeUtc { get; }
    public string Level { get; }
    public string Source { get; }
    public string EventId { get; }
    public string Message { get; }
}
