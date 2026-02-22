using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class GeoEnrichmentWorker : BackgroundService
{
    private readonly ILogger<GeoEnrichmentWorker> _logger;
    private readonly GeoEnrichmentQueue _queue;
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private readonly AppConfig _config;
    private readonly HttpClient _httpClient = new();
    private readonly Queue<DateTimeOffset> _requestTimestamps = new();
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeoEnrichmentWorker(
        ILogger<GeoEnrichmentWorker> logger,
        GeoEnrichmentQueue queue,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory,
        AppConfig config)
    {
        _logger = logger;
        _queue = queue;
        _dbContextFactory = dbContextFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var ip in _queue.DequeueAllAsync(stoppingToken))
        {
            if (!_config.EnrichmentEnabled)
            {
                continue;
            }

            try
            {
                await EnrichIpAsync(ip, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geo enrichment failed for {ip}.", ip);
            }
        }
    }

    private async Task EnrichIpAsync(string ip, CancellationToken cancellationToken)
    {
        await ThrottleAsync(cancellationToken);

        var url = string.IsNullOrWhiteSpace(_config.IpWhoisApiKey)
            ? $"https://ipwhois.io/{ip}"
            : $"https://ipwhois.io/{ip}?key={Uri.EscapeDataString(_config.IpWhoisApiKey)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await MarkFailureAsync(ip, cancellationToken);
            return;
        }

        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var payload = System.Text.Json.JsonSerializer.Deserialize<IpWhoisResponse>(rawJson, JsonOptions);
        if (payload == null)
        {
            await MarkFailureAsync(ip, cancellationToken);
            return;
        }

        payload.RawJson = rawJson;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.GeoCache.FirstOrDefaultAsync(g => g.Ip == ip, cancellationToken);
        if (existing == null)
        {
            existing = new GeoCache { Ip = ip };
            db.GeoCache.Add(existing);
        }

        existing.CountryCode = payload.CountryCode ?? string.Empty;
        existing.Country = payload.Country ?? string.Empty;
        existing.Region = payload.Region ?? string.Empty;
        existing.City = payload.City ?? string.Empty;
        existing.Asn = payload.Asn ?? string.Empty;
        existing.Isp = payload.Isp ?? string.Empty;
        existing.Lat = payload.Latitude;
        existing.Lon = payload.Longitude;
        existing.RawJson = payload.RawJson ?? string.Empty;
        existing.LastUpdated = DateTimeOffset.UtcNow;
        existing.NextRetryAt = null;
        existing.FailureCount = 0;
        existing.IsPartial = false;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailureAsync(string ip, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.GeoCache.FirstOrDefaultAsync(g => g.Ip == ip, cancellationToken);
        if (existing == null)
        {
            existing = new GeoCache { Ip = ip };
            db.GeoCache.Add(existing);
        }

        existing.FailureCount += 1;
        existing.NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(30);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        var limit = Math.Max(1, _config.EnrichmentMaxPerMinute);
        var now = DateTimeOffset.UtcNow;
        while (_requestTimestamps.Count > 0 && now - _requestTimestamps.Peek() > TimeSpan.FromMinutes(1))
        {
            _requestTimestamps.Dequeue();
        }

        if (_requestTimestamps.Count >= limit)
        {
            var wait = TimeSpan.FromSeconds(60) - (now - _requestTimestamps.Peek());
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken);
            }
        }

        _requestTimestamps.Enqueue(DateTimeOffset.UtcNow);
    }

    private sealed class IpWhoisResponse
    {
        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        [JsonPropertyName("region")]
        public string? Region { get; set; }
        [JsonPropertyName("city")]
        public string? City { get; set; }
        [JsonPropertyName("asn")]
        public string? Asn { get; set; }
        [JsonPropertyName("isp")]
        public string? Isp { get; set; }
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }
        public string? RawJson { get; set; }
    }
}
