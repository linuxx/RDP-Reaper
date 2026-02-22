using System;
using System.Collections.Concurrent;
using System.Linq;
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
                _firewallManager.AddBlockedEntry(ban.Key);
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
        _firewallManager.AddBlockedEntry(ip);

        _logger.LogInformation("Banned IP {ip} for {duration}.", ip, duration);
        return true;
    }

    public async Task<bool> ManualBanIpAsync(string ip, string reason, TimeSpan duration, CancellationToken cancellationToken)
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
            SourcePolicy = "Manual",
            LastSeenAt = now
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Bans.Add(ban);
        await db.SaveChangesAsync(cancellationToken);

        _activeBans.TryAdd(ip, ban);
        _statusState.SetActiveBans(_activeBans.Count);
        _firewallManager.AddBlockedEntry(ip);

        _logger.LogInformation("Manually banned IP {ip} for {duration}.", ip, duration);
        return true;
    }

    public async Task<bool> UnbanIpAsync(string ip, CancellationToken cancellationToken)
    {
        if (!_activeBans.TryRemove(ip, out _))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var ban = await db.Bans
            .OrderByDescending(b => b.BanId)
            .FirstOrDefaultAsync(b => b.Key == ip && (b.ExpiresAt == null || b.ExpiresAt > now), cancellationToken);
        if (ban != null)
        {
            ban.ExpiresAt = now;
            ban.Permanent = false;
            await db.SaveChangesAsync(cancellationToken);
        }

        _statusState.SetActiveBans(_activeBans.Count);
        _firewallManager.RemoveBlockedEntry(ip);
        _logger.LogInformation("Unbanned IP {ip}.", ip);
        return true;
    }

    public async Task<bool> BanSubnetAsync(string subnet, string reason, TimeSpan duration, CancellationToken cancellationToken)
    {
        if (IsBanned(subnet))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var ban = new Ban
        {
            BanType = "Subnet",
            Key = subnet,
            CreatedAt = now,
            ExpiresAt = duration == TimeSpan.Zero ? null : now.Add(duration),
            Permanent = duration == TimeSpan.Zero,
            Reason = reason,
            SourcePolicy = "SubnetThreshold",
            LastSeenAt = now
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Bans.Add(ban);
        await db.SaveChangesAsync(cancellationToken);

        _activeBans.TryAdd(subnet, ban);
        _statusState.SetActiveBans(_activeBans.Count);
        _firewallManager.AddBlockedEntry(subnet);

        _logger.LogWarning("Banned subnet {subnet} for {duration}.", subnet, duration);
        return true;
    }

    public async Task ExpireAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _activeBans.Values
            .Where(b => b.ExpiresAt.HasValue && b.ExpiresAt.Value <= now)
            .Select(b => b.Key)
            .ToList();

        if (expiredKeys.Count == 0)
        {
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var expiredBans = await db.Bans
            .Where(b => b.ExpiresAt.HasValue && b.ExpiresAt.Value <= now)
            .ToListAsync(cancellationToken);
        if (expiredBans.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var key in expiredKeys)
        {
            _activeBans.TryRemove(key, out _);
            _firewallManager.RemoveBlockedEntry(key);
        }

        _statusState.SetActiveBans(_activeBans.Count);
    }

    public IReadOnlyCollection<Ban> GetActiveBans()
    {
        return _activeBans.Values.ToArray();
    }
}
