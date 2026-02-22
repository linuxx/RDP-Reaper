using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using RdpReaper.Core.Config;
using RdpReaper.Core.Data;
using RdpReaper.Core.Security;
using RdpReaper.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => { options.ServiceName = "RdpReaper"; });
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog(settings =>
    {
        settings.SourceName = "RdpReaper";
        settings.LogName = "RdpReaper";
    });

    EnsureEventLogSource();
}

var appConfig = ConfigStore.LoadOrCreate();
Directory.CreateDirectory(Path.GetDirectoryName(appConfig.DatabasePath)!);
ApiSecretStore.GetOrCreateSecret();

builder.Services.AddSingleton(appConfig);
builder.Services.AddDbContextFactory<RdpReaperDbContext>(options =>
    options.UseSqlite($"Data Source={appConfig.DatabasePath}"));
builder.Services.AddSingleton<EventIngestor>();
builder.Services.AddSingleton<CounterStore>();
builder.Services.AddSingleton<StatusState>();
builder.Services.AddSingleton<BanManager>();
builder.Services.AddSingleton<AttemptProcessor>();
builder.Services.AddSingleton<FirewallManager>();
builder.Services.AddHostedService<ApiHost>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

static void EnsureEventLogSource()
{
    const string sourceName = "RdpReaper";
    const string logName = "RdpReaper";

    try
    {
        if (!EventLog.SourceExists(sourceName))
        {
            EventLog.CreateEventSource(sourceName, logName);
        }
    }
    catch
    {
        // If creating the event log source fails, keep running and fall back to other logs.
    }
}
