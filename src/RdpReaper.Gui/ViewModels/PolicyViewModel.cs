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
    private bool _firewallEnabled = true;
    private string _allowIpList = string.Empty;
    private string _blockIpList = string.Empty;
    private string _allowSubnetList = string.Empty;
    private string _blockSubnetList = string.Empty;
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
            FirewallEnabled = policy.FirewallEnabled;
            AllowIpList = string.Join(Environment.NewLine, policy.AllowIpList);
            BlockIpList = string.Join(Environment.NewLine, policy.BlockIpList);
            AllowSubnetList = string.Join(Environment.NewLine, policy.AllowSubnetList);
            BlockSubnetList = string.Join(Environment.NewLine, policy.BlockSubnetList);
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
                !int.TryParse(IpBanDurationSeconds, out var banSeconds))
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
                FirewallEnabled = FirewallEnabled,
                AllowIpList = SplitLines(AllowIpList),
                BlockIpList = SplitLines(BlockIpList),
                AllowSubnetList = SplitLines(AllowSubnetList),
                BlockSubnetList = SplitLines(BlockSubnetList)
            });

            IpFailureThreshold = updated.IpFailureThreshold.ToString();
            IpWindowSeconds = updated.IpWindowSeconds.ToString();
            IpBanDurationSeconds = updated.IpBanDurationSeconds.ToString();
            FirewallEnabled = updated.FirewallEnabled;
            AllowIpList = string.Join(Environment.NewLine, updated.AllowIpList);
            BlockIpList = string.Join(Environment.NewLine, updated.BlockIpList);
            AllowSubnetList = string.Join(Environment.NewLine, updated.AllowSubnetList);
            BlockSubnetList = string.Join(Environment.NewLine, updated.BlockSubnetList);
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
