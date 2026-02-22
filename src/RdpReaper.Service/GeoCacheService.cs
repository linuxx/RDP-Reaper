using Microsoft.EntityFrameworkCore;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class GeoCacheService
{
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private readonly AppConfig _config;

    public GeoCacheService(IDbContextFactory<RdpReaperDbContext> dbContextFactory, AppConfig config)
    {
        _dbContextFactory = dbContextFactory;
        _config = config;
    }

    public async Task<GeoCache?> GetAsync(string ip, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GeoCache.AsNoTracking().FirstOrDefaultAsync(g => g.Ip == ip, cancellationToken);
    }

    public bool IsFresh(GeoCache cache)
    {
        var ttl = TimeSpan.FromDays(_config.CacheTtlDays);
        return cache.LastUpdated > DateTimeOffset.UtcNow.Subtract(ttl);
    }
}
