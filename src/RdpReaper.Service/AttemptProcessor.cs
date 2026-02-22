using System;
using Microsoft.EntityFrameworkCore;
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

    public AttemptProcessor(
        ILogger<AttemptProcessor> logger,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory,
        AppConfig config,
        CounterStore counterStore,
        BanManager banManager,
        StatusState statusState)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _config = config;
        _counterStore = counterStore;
        _banManager = banManager;
        _statusState = statusState;
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
    }
}
