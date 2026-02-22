using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using RdpReaper.Core.Config;

namespace RdpReaper.Gui.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(AppConfig config, string token)
    {
        var baseAddress = $"http://{config.GuiServerAddress}:{config.GuiServerPort}/";
        _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
        _httpClient.DefaultRequestHeaders.Add("X-RdpReaper-Token", token);
    }

    public async Task<StatusResponse> GetStatusAsync()
    {
        var response = await _httpClient.GetAsync("api/status");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<StatusResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return status ?? new StatusResponse();
    }

    public async Task<IReadOnlyList<BanRecord>> GetBansAsync()
    {
        var response = await _httpClient.GetAsync("api/bans");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var bans = JsonSerializer.Deserialize<List<BanRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return bans ?? new List<BanRecord>();
    }

    public async Task<IReadOnlyList<AttemptRecord>> GetAttemptsAsync(int take)
    {
        return await GetAttemptsAsync(new AttemptQuery { Take = take });
    }

    public async Task<IReadOnlyList<AttemptRecord>> GetAttemptsAsync(AttemptQuery query)
    {
        var qs = BuildAttemptQuery(query);
        var response = await _httpClient.GetAsync($"api/attempts{qs}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var attempts = JsonSerializer.Deserialize<List<AttemptRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return attempts ?? new List<AttemptRecord>();
    }

    public async Task<PolicyDto> GetPolicyAsync()
    {
        var response = await _httpClient.GetAsync("api/policy");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var policy = JsonSerializer.Deserialize<PolicyDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return policy ?? new PolicyDto();
    }

    public async Task<PolicyDto> UpdatePolicyAsync(PolicyDto policy)
    {
        var json = JsonSerializer.Serialize(policy, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var response = await _httpClient.PutAsync("api/policy", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<PolicyDto>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return updated ?? policy;
    }

    public async Task<IReadOnlyList<LogEntry>> GetLogsAsync(int take)
    {
        var response = await _httpClient.GetAsync($"api/logs?take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var logs = JsonSerializer.Deserialize<List<LogEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return logs ?? new List<LogEntry>();
    }

    public async Task<StatsResponse> GetStatsAsync()
    {
        var response = await _httpClient.GetAsync("api/stats");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var stats = JsonSerializer.Deserialize<StatsResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return stats ?? new StatsResponse();
    }

    public async Task<IReadOnlyList<GeoRecord>> GetRecentGeoAsync(int take)
    {
        var response = await _httpClient.GetAsync($"api/geo/recent?take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var records = JsonSerializer.Deserialize<List<GeoRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return records ?? new List<GeoRecord>();
    }

    public async Task BanIpAsync(BanRequest request)
    {
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var response = await _httpClient.PostAsync("api/bans/ban", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public async Task UnbanIpAsync(string ip)
    {
        var json = JsonSerializer.Serialize(new UnbanRequest { Ip = ip }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var response = await _httpClient.PostAsync("api/bans/unban", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public sealed class StatusResponse
    {
        public string? Service { get; set; }
        public DateTimeOffset? LastEventUtc { get; set; }
        public int ActiveBans { get; set; }
    }

    public sealed class BanRecord
    {
        public string? BanType { get; set; }
        public string? Key { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public bool Permanent { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class AttemptRecord
    {
        public DateTimeOffset? Time { get; set; }
        public string? Ip { get; set; }
        public string? Subnet { get; set; }
        public string? Username { get; set; }
        public string? Outcome { get; set; }
        public int LogonType { get; set; }
        public string? Status { get; set; }
        public int EventId { get; set; }
    }

    public sealed class PolicyDto
    {
        public int IpFailureThreshold { get; set; }
        public int IpWindowSeconds { get; set; }
        public int IpBanDurationSeconds { get; set; }
        public int SubnetFailureThreshold { get; set; }
        public int SubnetWindowSeconds { get; set; }
        public int SubnetBanDurationSeconds { get; set; }
        public int SubnetMinUniqueIps { get; set; }
        public bool FirewallEnabled { get; set; }
        public List<string> AllowIpList { get; set; } = new();
        public List<string> BlockIpList { get; set; } = new();
        public List<string> AllowSubnetList { get; set; } = new();
        public List<string> BlockSubnetList { get; set; } = new();
        public List<string> AllowCountryList { get; set; } = new();
        public List<string> BlockCountryList { get; set; } = new();
        public bool EnrichmentEnabled { get; set; }
        public string IpWhoisApiKey { get; set; } = string.Empty;
        public int EnrichmentMaxPerMinute { get; set; }
        public int CacheTtlDays { get; set; }
        public List<int> MonitoredLogonTypes { get; set; } = new();
    }

    public sealed class LogEntry
    {
        public DateTimeOffset? TimeUtc { get; set; }
        public string? Level { get; set; }
        public string? Source { get; set; }
        public long EventId { get; set; }
        public string? Message { get; set; }
    }

    public sealed class StatsResponse
    {
        public int LastHourAttempts { get; set; }
        public int LastDayAttempts { get; set; }
        public int LastDayUniqueIps { get; set; }
        public int ActiveBans { get; set; }
    }

    public sealed class GeoRecord
    {
        public string? Ip { get; set; }
        public string? CountryCode { get; set; }
        public string? Country { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
    }

    public sealed class AttemptQuery
    {
        public int Take { get; set; } = 200;
        public int Skip { get; set; }
        public string? Ip { get; set; }
        public string? Username { get; set; }
        public string? Outcome { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }

    public sealed class BanRequest
    {
        public string Ip { get; set; } = string.Empty;
        public int DurationSeconds { get; set; } = 3600;
        public bool Permanent { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class UnbanRequest
    {
        public string Ip { get; set; } = string.Empty;
    }

    private static string BuildAttemptQuery(AttemptQuery query)
    {
        var parts = new List<string>
        {
            $"take={query.Take}",
            $"skip={query.Skip}"
        };

        if (!string.IsNullOrWhiteSpace(query.Ip))
        {
            parts.Add($"ip={Uri.EscapeDataString(query.Ip)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            parts.Add($"username={Uri.EscapeDataString(query.Username)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Outcome))
        {
            parts.Add($"outcome={Uri.EscapeDataString(query.Outcome)}");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            parts.Add($"status={Uri.EscapeDataString(query.Status)}");
        }

        if (query.From.HasValue)
        {
            parts.Add($"from={Uri.EscapeDataString(query.From.Value.ToString("o"))}");
        }

        if (query.To.HasValue)
        {
            parts.Add($"to={Uri.EscapeDataString(query.To.Value.ToString("o"))}");
        }

        return "?" + string.Join("&", parts);
    }
}
