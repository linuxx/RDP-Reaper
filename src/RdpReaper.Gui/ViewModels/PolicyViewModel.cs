using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class PolicyViewModel : INotifyPropertyChanged
{
    private string _ipFailureThreshold = string.Empty;
    private string _ipWindowSeconds = string.Empty;
    private string _ipBanDurationSeconds = string.Empty;
    private string _subnetFailureThreshold = string.Empty;
    private string _subnetWindowSeconds = string.Empty;
    private string _subnetBanDurationSeconds = string.Empty;
    private string _subnetMinUniqueIps = string.Empty;
    private bool _firewallEnabled = true;
    private string _allowIpList = string.Empty;
    private string _blockIpList = string.Empty;
    private string _allowSubnetList = string.Empty;
    private string _blockSubnetList = string.Empty;
    private string _allowCountryList = string.Empty;
    private string _blockCountryList = string.Empty;
    private bool _enrichmentEnabled = true;
    private string _ipWhoisApiKey = string.Empty;
    private string _enrichmentMaxPerMinute = string.Empty;
    private string _cacheTtlDays = string.Empty;
    private bool _monitorLogonType3 = true;
    private bool _monitorLogonType10 = true;
    private string _errorText = string.Empty;
    private bool _isBusy;

    public string IpFailureThreshold
    {
        get => _ipFailureThreshold;
        set => SetField(ref _ipFailureThreshold, value);
    }

    public string IpWindowSeconds
    {
        get => _ipWindowSeconds;
        set => SetField(ref _ipWindowSeconds, value);
    }

    public string IpBanDurationSeconds
    {
        get => _ipBanDurationSeconds;
        set => SetField(ref _ipBanDurationSeconds, value);
    }

    public string SubnetFailureThreshold
    {
        get => _subnetFailureThreshold;
        set => SetField(ref _subnetFailureThreshold, value);
    }

    public string SubnetWindowSeconds
    {
        get => _subnetWindowSeconds;
        set => SetField(ref _subnetWindowSeconds, value);
    }

    public string SubnetBanDurationSeconds
    {
        get => _subnetBanDurationSeconds;
        set => SetField(ref _subnetBanDurationSeconds, value);
    }

    public string SubnetMinUniqueIps
    {
        get => _subnetMinUniqueIps;
        set => SetField(ref _subnetMinUniqueIps, value);
    }

    public bool FirewallEnabled
    {
        get => _firewallEnabled;
        set => SetField(ref _firewallEnabled, value);
    }

    public string AllowIpList
    {
        get => _allowIpList;
        set => SetField(ref _allowIpList, value);
    }

    public string BlockIpList
    {
        get => _blockIpList;
        set => SetField(ref _blockIpList, value);
    }

    public string AllowSubnetList
    {
        get => _allowSubnetList;
        set => SetField(ref _allowSubnetList, value);
    }

    public string BlockSubnetList
    {
        get => _blockSubnetList;
        set => SetField(ref _blockSubnetList, value);
    }

    public string AllowCountryList
    {
        get => _allowCountryList;
        set => SetField(ref _allowCountryList, value);
    }

    public string BlockCountryList
    {
        get => _blockCountryList;
        set => SetField(ref _blockCountryList, value);
    }

    public bool EnrichmentEnabled
    {
        get => _enrichmentEnabled;
        set => SetField(ref _enrichmentEnabled, value);
    }

    public string IpWhoisApiKey
    {
        get => _ipWhoisApiKey;
        set => SetField(ref _ipWhoisApiKey, value);
    }

    public string EnrichmentMaxPerMinute
    {
        get => _enrichmentMaxPerMinute;
        set => SetField(ref _enrichmentMaxPerMinute, value);
    }

    public string CacheTtlDays
    {
        get => _cacheTtlDays;
        set => SetField(ref _cacheTtlDays, value);
    }

    public bool MonitorLogonType3
    {
        get => _monitorLogonType3;
        set => SetField(ref _monitorLogonType3, value);
    }

    public bool MonitorLogonType10
    {
        get => _monitorLogonType10;
        set => SetField(ref _monitorLogonType10, value);
    }

    public string ErrorText
    {
        get => _errorText;
        private set
        {
            if (SetField(ref _errorText, value))
            {
                OnPropertyChanged(nameof(ErrorVisibility));
            }
        }
    }

    public Visibility ErrorVisibility =>
        string.IsNullOrWhiteSpace(_errorText) ? Visibility.Collapsed : Visibility.Visible;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            var config = ConfigStore.LoadOrCreate();
            var token = ApiSecretStore.ReadSecret();
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorText = "API secret not found. Start the service to generate it.";
                return;
            }

            var client = new ApiClient(config, token);
            var policy = await client.GetPolicyAsync();

            IpFailureThreshold = policy.IpFailureThreshold.ToString();
            IpWindowSeconds = policy.IpWindowSeconds.ToString();
            IpBanDurationSeconds = policy.IpBanDurationSeconds.ToString();
            SubnetFailureThreshold = policy.SubnetFailureThreshold.ToString();
            SubnetWindowSeconds = policy.SubnetWindowSeconds.ToString();
            SubnetBanDurationSeconds = policy.SubnetBanDurationSeconds.ToString();
            SubnetMinUniqueIps = policy.SubnetMinUniqueIps.ToString();
            FirewallEnabled = policy.FirewallEnabled;
            AllowIpList = string.Join(Environment.NewLine, policy.AllowIpList);
            BlockIpList = string.Join(Environment.NewLine, policy.BlockIpList);
            AllowSubnetList = string.Join(Environment.NewLine, policy.AllowSubnetList);
            BlockSubnetList = string.Join(Environment.NewLine, policy.BlockSubnetList);
            AllowCountryList = string.Join(Environment.NewLine, policy.AllowCountryList);
            BlockCountryList = string.Join(Environment.NewLine, policy.BlockCountryList);
            EnrichmentEnabled = policy.EnrichmentEnabled;
            IpWhoisApiKey = policy.IpWhoisApiKey;
            EnrichmentMaxPerMinute = policy.EnrichmentMaxPerMinute.ToString();
            CacheTtlDays = policy.CacheTtlDays.ToString();
            MonitorLogonType3 = policy.MonitoredLogonTypes.Contains(3);
            MonitorLogonType10 = policy.MonitoredLogonTypes.Contains(10);
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to load policy: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveAsync()
    {
        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            if (!int.TryParse(IpFailureThreshold, out var failureThreshold) ||
                !int.TryParse(IpWindowSeconds, out var windowSeconds) ||
                !int.TryParse(IpBanDurationSeconds, out var banSeconds) ||
                !int.TryParse(SubnetFailureThreshold, out var subnetFailureThreshold) ||
                !int.TryParse(SubnetWindowSeconds, out var subnetWindowSeconds) ||
                !int.TryParse(SubnetBanDurationSeconds, out var subnetBanSeconds) ||
                !int.TryParse(SubnetMinUniqueIps, out var subnetMinUniqueIps) ||
                !int.TryParse(EnrichmentMaxPerMinute, out var maxPerMinute) ||
                !int.TryParse(CacheTtlDays, out var cacheTtlDays))
            {
                ErrorText = "Enter valid numeric values for policy thresholds.";
                return;
            }

            var config = ConfigStore.LoadOrCreate();
            var token = ApiSecretStore.ReadSecret();
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorText = "API secret not found. Start the service to generate it.";
                return;
            }

            var client = new ApiClient(config, token);
            var updated = await client.UpdatePolicyAsync(new ApiClient.PolicyDto
            {
                IpFailureThreshold = failureThreshold,
                IpWindowSeconds = windowSeconds,
                IpBanDurationSeconds = banSeconds,
                SubnetFailureThreshold = subnetFailureThreshold,
                SubnetWindowSeconds = subnetWindowSeconds,
                SubnetBanDurationSeconds = subnetBanSeconds,
                SubnetMinUniqueIps = subnetMinUniqueIps,
                FirewallEnabled = FirewallEnabled,
                AllowIpList = SplitLines(AllowIpList),
                BlockIpList = SplitLines(BlockIpList),
                AllowSubnetList = SplitLines(AllowSubnetList),
                BlockSubnetList = SplitLines(BlockSubnetList),
                AllowCountryList = SplitLines(AllowCountryList),
                BlockCountryList = SplitLines(BlockCountryList),
                EnrichmentEnabled = EnrichmentEnabled,
                IpWhoisApiKey = IpWhoisApiKey,
                EnrichmentMaxPerMinute = maxPerMinute,
                CacheTtlDays = cacheTtlDays,
                MonitoredLogonTypes = BuildLogonTypeList()
            });

            IpFailureThreshold = updated.IpFailureThreshold.ToString();
            IpWindowSeconds = updated.IpWindowSeconds.ToString();
            IpBanDurationSeconds = updated.IpBanDurationSeconds.ToString();
            SubnetFailureThreshold = updated.SubnetFailureThreshold.ToString();
            SubnetWindowSeconds = updated.SubnetWindowSeconds.ToString();
            SubnetBanDurationSeconds = updated.SubnetBanDurationSeconds.ToString();
            SubnetMinUniqueIps = updated.SubnetMinUniqueIps.ToString();
            FirewallEnabled = updated.FirewallEnabled;
            AllowIpList = string.Join(Environment.NewLine, updated.AllowIpList);
            BlockIpList = string.Join(Environment.NewLine, updated.BlockIpList);
            AllowSubnetList = string.Join(Environment.NewLine, updated.AllowSubnetList);
            BlockSubnetList = string.Join(Environment.NewLine, updated.BlockSubnetList);
            AllowCountryList = string.Join(Environment.NewLine, updated.AllowCountryList);
            BlockCountryList = string.Join(Environment.NewLine, updated.BlockCountryList);
            EnrichmentEnabled = updated.EnrichmentEnabled;
            IpWhoisApiKey = updated.IpWhoisApiKey;
            EnrichmentMaxPerMinute = updated.EnrichmentMaxPerMinute.ToString();
            CacheTtlDays = updated.CacheTtlDays.ToString();
            MonitorLogonType3 = updated.MonitoredLogonTypes.Contains(3);
            MonitorLogonType10 = updated.MonitoredLogonTypes.Contains(10);
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to save policy: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static List<string> SplitLines(string value)
    {
        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private List<int> BuildLogonTypeList()
    {
        var list = new List<int>();
        if (MonitorLogonType3)
        {
            list.Add(3);
        }
        if (MonitorLogonType10)
        {
            list.Add(10);
        }
        return list;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
