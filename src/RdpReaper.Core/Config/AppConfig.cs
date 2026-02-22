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

    public bool FirewallEnabled { get; set; } = true;
}
