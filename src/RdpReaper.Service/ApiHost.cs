using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;

namespace RdpReaper.Service;

public sealed class ApiHost : IHostedService
{
    private readonly ILogger<ApiHost> _logger;
    private readonly AppConfig _config;
    private readonly BanManager _banManager;
    private readonly StatusState _statusState;
    private WebApplication? _app;
    private string? _secret;

    public ApiHost(
        ILogger<ApiHost> logger,
        AppConfig config,
        BanManager banManager,
        StatusState statusState)
    {
        _logger = logger;
        _config = config;
        _banManager = banManager;
        _statusState = statusState;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _secret = ApiSecretStore.ReadSecret() ?? ApiSecretStore.GetOrCreateSecret();

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
}
