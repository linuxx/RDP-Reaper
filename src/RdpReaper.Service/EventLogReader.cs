using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RdpReaper.Service;

public static class EventLogReader
{
    public static IReadOnlyList<LogEntry> ReadRecent(string logName, int take)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<LogEntry>();
        }

        var entries = new List<LogEntry>();
        try
        {
            using var log = new EventLog(logName);
            for (var i = log.Entries.Count - 1; i >= 0 && entries.Count < take; i--)
            {
                var entry = log.Entries[i];
                entries.Add(new LogEntry
                {
                    TimeUtc = entry.TimeWritten.ToUniversalTime(),
                    Level = entry.EntryType.ToString(),
                    Source = entry.Source,
                    EventId = entry.InstanceId,
                    Message = entry.Message
                });
            }
        }
        catch
        {
            return Array.Empty<LogEntry>();
        }

        return entries;
    }

    public sealed class LogEntry
    {
        public DateTimeOffset TimeUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public long EventId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
