using Microsoft.EntityFrameworkCore;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;

namespace RdpReaper.Service;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDbContextFactory<RdpReaperDbContext> _dbContextFactory;
    private readonly AppConfig _config;
    private readonly EventIngestor _eventIngestor;
    private readonly FirewallManager _firewallManager;
    private readonly BanManager _banManager;

    public Worker(
        ILogger<Worker> logger,
        IDbContextFactory<RdpReaperDbContext> dbContextFactory,
        AppConfig config,
        EventIngestor eventIngestor,
        FirewallManager firewallManager,
        BanManager banManager)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _config = config;
        _eventIngestor = eventIngestor;
        _firewallManager = firewallManager;
        _banManager = banManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RdpReaper service started. Api={address}:{port}", _config.ApiListenAddress, _config.ApiListenPort);

        await using (var db = await _dbContextFactory.CreateDbContextAsync(stoppingToken))
        {
            await db.Database.EnsureCreatedAsync(stoppingToken);
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", stoppingToken);
        }

        await _banManager.InitializeAsync(stoppingToken);
        _firewallManager.Initialize();
        _eventIngestor.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        finally
        {
            _eventIngestor.Stop();
            _firewallManager.Shutdown();
            _logger.LogInformation("RdpReaper service stopped.");
        }
    }
}
