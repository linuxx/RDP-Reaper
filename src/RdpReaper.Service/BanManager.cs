using System;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class BanManager
{
    private readonly ILogger<BanManager> _logger;
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private readonly FirewallManager _firewallManager;
    private readonly StatusState _statusState;
    private readonly ConcurrentDictionary<string, Ban> _activeBans = new(StringComparer.OrdinalIgnoreCase);

    public BanManager(
        ILogger<BanManager> logger,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory,
        FirewallManager firewallManager,
        StatusState statusState)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _firewallManager = firewallManager;
        _statusState = statusState;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var allBans = await db.Bans
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var active = allBans
            .Where(b => b.ExpiresAt == null || b.ExpiresAt > now)
            .ToList();

        foreach (var ban in active)
        {
            if (_activeBans.TryAdd(ban.Key, ban))
            {
                _firewallManager.AddBlockedIp(ban.Key);
            }
        }

        _statusState.SetActiveBans(_activeBans.Count);
    }

    public bool IsBanned(string ip)
    {
        return _activeBans.ContainsKey(ip);
    }

    public async Task<bool> TryBanIpAsync(string ip, string reason, TimeSpan duration, CancellationToken cancellationToken)
    {
        if (IsBanned(ip))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var ban = new Ban
        {
            BanType = "Ip",
            Key = ip,
            CreatedAt = now,
            ExpiresAt = duration == TimeSpan.Zero ? null : now.Add(duration),
            Permanent = duration == TimeSpan.Zero,
            Reason = reason,
            SourcePolicy = "IpThreshold",
            LastSeenAt = now
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Bans.Add(ban);
        await db.SaveChangesAsync(cancellationToken);

        _activeBans.TryAdd(ip, ban);
        _statusState.SetActiveBans(_activeBans.Count);
        _firewallManager.AddBlockedIp(ip);

        _logger.LogInformation("Banned IP {ip} for {duration}.", ip, duration);
        return true;
    }

    public IReadOnlyCollection<Ban> GetActiveBans()
    {
        return _activeBans.Values.ToArray();
    }
}
