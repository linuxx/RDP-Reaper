namespace RdpReaper.Service;

public sealed class BanExpiryWorker : BackgroundService
{
    private readonly ILogger<BanExpiryWorker> _logger;
    private readonly BanManager _banManager;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

    public BanExpiryWorker(ILogger<BanExpiryWorker> logger, BanManager banManager)
    {
        _logger = logger;
        _banManager = banManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _banManager.ExpireAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ban expiry check failed.");
            }
        }
    }
}
