using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class AttemptProcessor
{
    private readonly ILogger<AttemptProcessor> _logger;
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private readonly AppConfig _config;
    private readonly CounterStore _counterStore;
    private readonly BanManager _banManager;
    private readonly StatusState _statusState;
    private readonly GeoCacheService _geoCacheService;
    private readonly GeoEnrichmentQueue _geoQueue;

    public AttemptProcessor(
        ILogger<AttemptProcessor> logger,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory,
        AppConfig config,
        CounterStore counterStore,
        BanManager banManager,
        StatusState statusState,
        GeoCacheService geoCacheService,
        GeoEnrichmentQueue geoQueue)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _config = config;
        _counterStore = counterStore;
        _banManager = banManager;
        _statusState = statusState;
        _geoCacheService = geoCacheService;
        _geoQueue = geoQueue;
    }

    public async Task ProcessAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        _statusState.UpdateLastEvent(attempt.Time);

        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Attempts.Add(attempt);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!string.Equals(attempt.Outcome, "Failure", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (IsAllowListed(attempt))
        {
            return;
        }

        if (IsBlockListed(attempt))
        {
            await _banManager.ManualBanIpAsync(
                attempt.Ip,
                "Block list entry",
                TimeSpan.FromSeconds(_config.IpBanDurationSeconds),
                cancellationToken);
            return;
        }

        await ApplyCountryPolicyAsync(attempt, cancellationToken);
        await QueueEnrichmentAsync(attempt, cancellationToken);

        var window = TimeSpan.FromSeconds(_config.IpWindowSeconds);
        var count = _counterStore.AddFailure(attempt.Ip, attempt.Time, window);
        if (count >= _config.IpFailureThreshold)
        {
            var duration = TimeSpan.FromSeconds(_config.IpBanDurationSeconds);
            var banned = await _banManager.TryBanIpAsync(
                attempt.Ip,
                $"IP threshold exceeded ({count} in {window.TotalSeconds}s)",
                duration,
                cancellationToken);

            if (banned)
            {
                _logger.LogWarning("IP {ip} banned after {count} failures.", attempt.Ip, count);
            }
        }

        if (!string.IsNullOrWhiteSpace(attempt.Subnet))
        {
            var subnetWindow = TimeSpan.FromSeconds(_config.SubnetWindowSeconds);
            var (total, unique) = _counterStore.AddSubnetFailure(attempt.Subnet, attempt.Ip, attempt.Time, subnetWindow);
            if (total >= _config.SubnetFailureThreshold && unique >= _config.SubnetMinUniqueIps)
            {
                var duration = TimeSpan.FromSeconds(_config.SubnetBanDurationSeconds);
                await _banManager.BanSubnetAsync(
                    attempt.Subnet,
                    $"Subnet threshold exceeded ({total} failures, {unique} IPs)",
                    duration,
                    cancellationToken);
            }
        }
    }

    private bool IsAllowListed(Attempt attempt)
    {
        return Contains(_config.AllowIpList, attempt.Ip) ||
               Contains(_config.AllowSubnetList, attempt.Subnet);
    }

    private bool IsBlockListed(Attempt attempt)
    {
        return Contains(_config.BlockIpList, attempt.Ip) ||
               Contains(_config.BlockSubnetList, attempt.Subnet);
    }

    private async Task ApplyCountryPolicyAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        if (_config.AllowCountryList.Count == 0 && _config.BlockCountryList.Count == 0)
        {
            return;
        }

        var cache = await _geoCacheService.GetAsync(attempt.Ip, cancellationToken);
        if (cache == null || string.IsNullOrWhiteSpace(cache.CountryCode))
        {
            return;
        }

        var country = cache.CountryCode;
        if (_config.AllowCountryList.Count > 0 && !Contains(_config.AllowCountryList, country))
        {
            await _banManager.ManualBanIpAsync(
                attempt.Ip,
                $"Country not allowed: {country}",
                TimeSpan.FromSeconds(_config.IpBanDurationSeconds),
                cancellationToken);
            return;
        }

        if (Contains(_config.BlockCountryList, country))
        {
            await _banManager.ManualBanIpAsync(
                attempt.Ip,
                $"Country blocked: {country}",
                TimeSpan.FromSeconds(_config.IpBanDurationSeconds),
                cancellationToken);
        }
    }

    private async Task QueueEnrichmentAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        if (!_config.EnrichmentEnabled)
        {
            return;
        }

        var cache = await _geoCacheService.GetAsync(attempt.Ip, cancellationToken);
        if (cache != null && cache.NextRetryAt.HasValue && cache.NextRetryAt.Value > DateTimeOffset.UtcNow)
        {
            return;
        }

        if (cache == null || !_geoCacheService.IsFresh(cache))
        {
            await _geoQueue.EnqueueAsync(attempt.Ip);
        }
    }

    private static bool Contains(IEnumerable<string> list, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return list.Any(entry => string.Equals(entry, value, StringComparison.OrdinalIgnoreCase));
    }
}
