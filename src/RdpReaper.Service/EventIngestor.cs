using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Net;
using System.Xml.Linq;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class EventIngestor
{
    private const int FailedLogonEventId = 4625;
    private readonly ILogger<EventIngestor> _logger;
    private readonly AttemptProcessor _attemptProcessor;
    private readonly AppConfig _config;
    private EventLogWatcher? _watcher;

    public EventIngestor(ILogger<EventIngestor> logger, AttemptProcessor attemptProcessor, AppConfig config)
    {
        _logger = logger;
        _attemptProcessor = attemptProcessor;
        _config = config;
    }

    public void Start()
    {
        var logonTypes = _config.MonitoredLogonTypes.Count == 0 ? new List<int> { 3, 10 } : _config.MonitoredLogonTypes;
        var logonTypeFilter = string.Join(" or ", logonTypes.Select(t => $"*[EventData[Data[@Name='LogonType']='{t}']]"));
        var query = $"*[System[(EventID=4625)]] and ({logonTypeFilter})";
        var logQuery = new EventLogQuery("Security", PathType.LogName, query);
        _watcher = new EventLogWatcher(logQuery);
        _watcher.EventRecordWritten += OnEventRecordWritten;
        _watcher.Enabled = true;

        _logger.LogInformation("Event ingestion initialized for Security log.");
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EventRecordWritten -= OnEventRecordWritten;
            _watcher.Enabled = false;
            _watcher.Dispose();
            _watcher = null;
        }

        _logger.LogInformation("Event ingestion stopped.");
    }

    private void OnEventRecordWritten(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventRecord == null)
        {
            return;
        }

        try
        {
            var attempt = ParseAttempt(e.EventRecord);
            if (attempt == null)
            {
                return;
            }

            _ = _attemptProcessor.ProcessAsync(attempt, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Security event.");
        }
        finally
        {
            e.EventRecord.Dispose();
        }
    }

    private Attempt? ParseAttempt(EventRecord record)
    {
        if (record.Id != FailedLogonEventId)
        {
            return null;
        }

        var xml = XDocument.Parse(record.ToXml());
        var logonType = GetEventDataValue(xml, "LogonType");
        if (!int.TryParse(logonType, NumberStyles.Integer, CultureInfo.InvariantCulture, out var type))
        {
            return null;
        }

        var allowed = _config.MonitoredLogonTypes.Count == 0 ? new List<int> { 3, 10 } : _config.MonitoredLogonTypes;
        if (!allowed.Contains(type))
        {
            return null;
        }

        var ip = GetEventDataValue(xml, "IpAddress");
        if (string.IsNullOrWhiteSpace(ip) || ip == "-")
        {
            return null;
        }

        var attempt = new Attempt
        {
            Time = record.TimeCreated.HasValue
                ? new DateTimeOffset(record.TimeCreated.Value)
                : DateTimeOffset.UtcNow,
            Ip = ip,
            Subnet = GetSubnet(ip),
            Username = GetEventDataValue(xml, "TargetUserName") ?? string.Empty,
            Outcome = "Failure",
            LogonType = type,
            Status = GetStatus(record, xml),
            EventId = record.Id
        };

        return attempt;
    }

    private static string GetStatus(EventRecord record, XDocument xml)
    {
        var status = GetEventDataValue(xml, "Status") ?? string.Empty;
        var subStatus = GetEventDataValue(xml, "SubStatus") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(subStatus))
        {
            return status;
        }

        return $"{status}:{subStatus}";
    }

    private static string? GetEventDataValue(XDocument xml, string name)
    {
        var ns = xml.Root?.Name.Namespace ?? XNamespace.None;
        var element = xml.Root?
            .Element(ns + "EventData")?
            .Elements(ns + "Data")
            .FirstOrDefault(x => string.Equals((string?)x.Attribute("Name"), name, StringComparison.OrdinalIgnoreCase));

        return element?.Value;
    }

    private static string GetSubnet(string ip)
    {
        if (!IPAddress.TryParse(ip, out var address))
        {
            return string.Empty;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
        }

        return string.Empty;
    }
}
