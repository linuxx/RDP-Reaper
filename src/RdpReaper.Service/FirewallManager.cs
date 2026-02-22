using System.Collections.Concurrent;
using System.Diagnostics;
using RdpReaper.Core.Config;

namespace RdpReaper.Service;

public sealed class FirewallManager
{
    private const string RulePrefix = "RdpReaper Block List";
    private const int MaxAddressesPerRule = 1000;
    private readonly ILogger<FirewallManager> _logger;
    private readonly AppConfig _config;
    private readonly ConcurrentDictionary<string, byte> _blockedIps = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(5);
    private readonly CancellationTokenSource _shutdown = new();
    private Task? _syncTask;
    private int _dirty;

    public FirewallManager(ILogger<FirewallManager> logger, AppConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public void Initialize()
    {
        _logger.LogInformation("Firewall manager initialized for RDP port 3389.");
        _syncTask = Task.Run(() => SyncLoopAsync(_shutdown.Token));
    }

    public void Shutdown()
    {
        _shutdown.Cancel();
        _syncTask?.GetAwaiter().GetResult();
        _logger.LogInformation("Firewall manager stopped.");
    }

    public void AddBlockedIp(string ip)
    {
        if (_blockedIps.TryAdd(ip, 0))
        {
            Interlocked.Exchange(ref _dirty, 1);
        }
    }

    private async Task SyncLoopAsync(CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(_syncInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (Interlocked.Exchange(ref _dirty, 0) == 0)
            {
                continue;
            }

            if (!_config.FirewallEnabled)
            {
                _logger.LogInformation("Firewall sync skipped (disabled).");
                continue;
            }

            try
            {
                SyncFirewallRules();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firewall sync failed.");
            }
        }
    }

    private void SyncFirewallRules()
    {
        var ips = _blockedIps.Keys.OrderBy(ip => ip, StringComparer.OrdinalIgnoreCase).ToArray();
        var chunks = ips.Chunk(MaxAddressesPerRule).ToArray();

        RunNetsh($"advfirewall firewall delete rule name=\"{RulePrefix}*\"");
        if (chunks.Length == 0)
        {
            return;
        }

        for (var i = 0; i < chunks.Length; i++)
        {
            var ruleName = $"{RulePrefix} {i + 1:000}";
            var addressList = string.Join(",", chunks[i]);
            var args =
                $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=block protocol=TCP localport=3389 remoteip={addressList}";
            RunNetsh(args);
        }

        _logger.LogInformation("Firewall rules updated with {count} blocked IPs.", ips.Length);
    }

    private void RunNetsh(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit(5000);
    }
}
