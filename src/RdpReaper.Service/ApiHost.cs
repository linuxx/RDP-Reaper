using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;
using RdpReaper.Core.Security;

namespace RdpReaper.Service;

public sealed class ApiHost : IHostedService
{
    private readonly ILogger<ApiHost> _logger;
    private readonly AppConfig _config;
    private readonly BanManager _banManager;
    private readonly StatusState _statusState;
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private WebApplication? _app;
    private string? _secret;

    public ApiHost(
        ILogger<ApiHost> logger,
        AppConfig config,
        BanManager banManager,
        StatusState statusState,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory)
    {
        _logger = logger;
        _config = config;
        _banManager = banManager;
        _statusState = statusState;
        _dbContextFactory = dbContextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _secret = ApiSecretStore.ReadSecret() ?? ApiSecretStore.GetOrCreateSecret();

        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{_config.ApiListenAddress}:{_config.ApiListenPort}");

        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-RdpReaper-Token", out var token) ||
                !string.Equals(token, _secret, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            await next();
        });

        app.MapGet("/api/status", () =>
        {
            return Results.Ok(new
            {
                service = "RdpReaper",
                lastEventUtc = _statusState.GetLastEvent(),
                activeBans = _statusState.GetActiveBans()
            });
        });

        app.MapGet("/api/bans", () =>
        {
            return Results.Ok(_banManager.GetActiveBans());
        });

        app.MapPost("/api/bans/ban", async (BanRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Ip))
            {
                return Results.BadRequest("IP is required.");
            }

            var duration = request.Permanent ? TimeSpan.Zero : TimeSpan.FromSeconds(request.DurationSeconds);
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Manual ban" : request.Reason;
            var success = await _banManager.ManualBanIpAsync(request.Ip, reason, duration, cancellationToken);
            return success ? Results.Ok() : Results.Conflict("IP already banned.");
        });

        app.MapPost("/api/bans/unban", async (UnbanRequest request, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Ip))
            {
                return Results.BadRequest("IP is required.");
            }

            var success = await _banManager.UnbanIpAsync(request.Ip, cancellationToken);
            return success ? Results.Ok() : Results.NotFound("IP not found.");
        });

        app.MapGet("/api/attempts", async (HttpRequest request, CancellationToken cancellationToken) =>
        {
            var take = GetTake(request, 200);
            var skip = GetSkip(request);
            var ip = request.Query["ip"].ToString();
            var username = request.Query["username"].ToString();
            var outcome = request.Query["outcome"].ToString();
            var status = request.Query["status"].ToString();
            var from = ParseDate(request.Query["from"]);
            var to = ParseDate(request.Query["to"]);

            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var query = db.Attempts.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(ip))
            {
                query = query.Where(a => a.Ip.Contains(ip));
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(a => a.Username.Contains(username));
            }

            if (!string.IsNullOrWhiteSpace(outcome))
            {
                query = query.Where(a => a.Outcome == outcome);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status.Contains(status));
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.Time >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.Time <= to.Value);
            }

            var attempts = await query
                .OrderByDescending(a => a.AttemptId)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return Results.Ok(attempts);
        });

        app.MapGet("/api/policy", () =>
        {
            var current = ConfigStore.LoadOrCreate();
            return Results.Ok(new PolicyDto
            {
                IpFailureThreshold = current.IpFailureThreshold,
                IpWindowSeconds = current.IpWindowSeconds,
                IpBanDurationSeconds = current.IpBanDurationSeconds,
                FirewallEnabled = current.FirewallEnabled,
                AllowIpList = current.AllowIpList,
                BlockIpList = current.BlockIpList,
                AllowSubnetList = current.AllowSubnetList,
                BlockSubnetList = current.BlockSubnetList
            });
        });

        app.MapPut("/api/policy", (PolicyUpdate update) =>
        {
            var current = ConfigStore.LoadOrCreate();
            current.IpFailureThreshold = update.IpFailureThreshold;
            current.IpWindowSeconds = update.IpWindowSeconds;
            current.IpBanDurationSeconds = update.IpBanDurationSeconds;
            current.FirewallEnabled = update.FirewallEnabled;
            current.AllowIpList = update.AllowIpList ?? new List<string>();
            current.BlockIpList = update.BlockIpList ?? new List<string>();
            current.AllowSubnetList = update.AllowSubnetList ?? new List<string>();
            current.BlockSubnetList = update.BlockSubnetList ?? new List<string>();

            var normalized = NormalizePolicy(current);
            ConfigStore.Save(normalized);
            _logger.LogInformation("Policy updated via API.");
            return Results.Ok(new PolicyDto
            {
                IpFailureThreshold = normalized.IpFailureThreshold,
                IpWindowSeconds = normalized.IpWindowSeconds,
                IpBanDurationSeconds = normalized.IpBanDurationSeconds,
                FirewallEnabled = normalized.FirewallEnabled,
                AllowIpList = normalized.AllowIpList,
                BlockIpList = normalized.BlockIpList,
                AllowSubnetList = normalized.AllowSubnetList,
                BlockSubnetList = normalized.BlockSubnetList
            });
        });

        app.MapGet("/api/logs", (HttpRequest request) =>
        {
            var take = GetTake(request, 200);
            var logs = EventLogReader.ReadRecent("RdpReaper", take);
            return Results.Ok(logs);
        });

        _app = app;
        await app.StartAsync(cancellationToken);
        _logger.LogInformation("Local API listening on {address}:{port}.", _config.ApiListenAddress, _config.ApiListenPort);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app == null)
        {
            return;
        }

        await _app.StopAsync(cancellationToken);
        await _app.DisposeAsync();
        _app = null;
    }

    private static int GetTake(HttpRequest request, int defaultTake)
    {
        if (int.TryParse(request.Query["take"], out var take) && take > 0 && take <= 1000)
        {
            return take;
        }

        return defaultTake;
    }

    private static int GetSkip(HttpRequest request)
    {
        if (int.TryParse(request.Query["skip"], out var skip) && skip >= 0 && skip <= 100000)
        {
            return skip;
        }

        return 0;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && DateTimeOffset.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static AppConfig NormalizePolicy(AppConfig config)
    {
        config.IpFailureThreshold = Math.Clamp(config.IpFailureThreshold, 1, 1000);
        config.IpWindowSeconds = Math.Clamp(config.IpWindowSeconds, 10, 3600);
        config.IpBanDurationSeconds = Math.Clamp(config.IpBanDurationSeconds, 60, 604800);
        return config;
    }

    private sealed class PolicyUpdate
    {
        public int IpFailureThreshold { get; set; }
        public int IpWindowSeconds { get; set; }
        public int IpBanDurationSeconds { get; set; }
        public bool FirewallEnabled { get; set; }
        public List<string>? AllowIpList { get; set; }
        public List<string>? BlockIpList { get; set; }
        public List<string>? AllowSubnetList { get; set; }
        public List<string>? BlockSubnetList { get; set; }
    }

    private sealed class PolicyDto
    {
        public int IpFailureThreshold { get; set; }
        public int IpWindowSeconds { get; set; }
        public int IpBanDurationSeconds { get; set; }
        public bool FirewallEnabled { get; set; }
        public List<string> AllowIpList { get; set; } = new();
        public List<string> BlockIpList { get; set; } = new();
        public List<string> AllowSubnetList { get; set; } = new();
        public List<string> BlockSubnetList { get; set; } = new();
    }

    private sealed class BanRequest
    {
        public string Ip { get; set; } = string.Empty;
        public int DurationSeconds { get; set; } = 3600;
        public bool Permanent { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class UnbanRequest
    {
        public string Ip { get; set; } = string.Empty;
    }
}
