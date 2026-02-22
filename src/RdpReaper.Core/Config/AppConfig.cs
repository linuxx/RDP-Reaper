namespace RdpReaper.Core.Config;

public sealed class AppConfig
{
    public string ApiListenAddress { get; set; } = "127.0.0.1";
    public int ApiListenPort { get; set; } = 5055;

    public string GuiServerAddress { get; set; } = "127.0.0.1";
    public int GuiServerPort { get; set; } = 5055;

    public string DatabasePath { get; set; } = string.Empty;

    public int IpFailureThreshold { get; set; } = 8;
    public int IpWindowSeconds { get; set; } = 120;
    public int IpBanDurationSeconds { get; set; } = 3600;

    public int SubnetFailureThreshold { get; set; } = 20;
    public int SubnetWindowSeconds { get; set; } = 300;
    public int SubnetBanDurationSeconds { get; set; } = 7200;
    public int SubnetMinUniqueIps { get; set; } = 3;

    public bool FirewallEnabled { get; set; } = true;

    public List<string> AllowIpList { get; set; } = new();
    public List<string> BlockIpList { get; set; } = new();
    public List<string> AllowSubnetList { get; set; } = new();
    public List<string> BlockSubnetList { get; set; } = new();

    public List<string> AllowCountryList { get; set; } = new();
    public List<string> BlockCountryList { get; set; } = new();

    public bool EnrichmentEnabled { get; set; } = true;
    public string IpWhoisApiKey { get; set; } = string.Empty;
    public int EnrichmentMaxPerMinute { get; set; } = 20;
    public int CacheTtlDays { get; set; } = 30;

    public List<int> MonitoredLogonTypes { get; set; } = new() { 3, 10 };
}
