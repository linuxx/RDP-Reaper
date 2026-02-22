using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class StatusViewModel : INotifyPropertyChanged
{
    private string _serviceText = "Unknown";
    private string _lastEventText = "-";
    private string _activeBansText = "0";
    private string _errorText = string.Empty;
    private bool _isBusy;
    private string _lastHourAttempts = "0";
    private string _lastDayAttempts = "0";
    private string _lastDayUniqueIps = "0";

    public string ServiceText
    {
        get => _serviceText;
        private set => SetField(ref _serviceText, value);
    }

    public string LastEventText
    {
        get => _lastEventText;
        private set => SetField(ref _lastEventText, value);
    }

    public string ActiveBansText
    {
        get => _activeBansText;
        private set => SetField(ref _activeBansText, value);
    }

    public string LastHourAttemptsText
    {
        get => _lastHourAttempts;
        private set => SetField(ref _lastHourAttempts, value);
    }

    public string LastDayAttemptsText
    {
        get => _lastDayAttempts;
        private set => SetField(ref _lastDayAttempts, value);
    }

    public string LastDayUniqueIpsText
    {
        get => _lastDayUniqueIps;
        private set => SetField(ref _lastDayUniqueIps, value);
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

    public async Task RefreshAsync()
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
            var status = await client.GetStatusAsync();
            var stats = await client.GetStatsAsync();

            ServiceText = status.Service ?? "Unknown";
            LastEventText = status.LastEventUtc?.ToString("u") ?? "-";
            ActiveBansText = status.ActiveBans.ToString();
            LastHourAttemptsText = stats.LastHourAttempts.ToString();
            LastDayAttemptsText = stats.LastDayAttempts.ToString();
            LastDayUniqueIpsText = stats.LastDayUniqueIps.ToString();
        }
        catch (Exception ex)
        {
            ErrorText = $"Failed to load status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
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
