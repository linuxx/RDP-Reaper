using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class BansViewModel : INotifyPropertyChanged
{
    public ObservableCollection<BanView> Bans { get; } = new();

    private string _manualIp = string.Empty;
    private string _manualDurationSeconds = "3600";
    private string _manualReason = "Manual ban";
    private bool _manualPermanent;
    private string _errorText = string.Empty;
    private bool _isBusy;

    public string ManualIp
    {
        get => _manualIp;
        set => SetField(ref _manualIp, value);
    }

    public string ManualDurationSeconds
    {
        get => _manualDurationSeconds;
        set => SetField(ref _manualDurationSeconds, value);
    }

    public string ManualReason
    {
        get => _manualReason;
        set => SetField(ref _manualReason, value);
    }

    public bool ManualPermanent
    {
        get => _manualPermanent;
        set => SetField(ref _manualPermanent, value);
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
            var bans = await client.GetBansAsync();

            Bans.Clear();
            foreach (var ban in bans)
            {
                Bans.Add(new BanView(ban));
            }
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to load bans: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task BanAsync()
    {
        IsBusy = true;
        ErrorText = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(ManualIp))
            {
                ErrorText = "Enter an IP to ban.";
                return;
            }

            if (!int.TryParse(ManualDurationSeconds, out var seconds))
            {
                ErrorText = "Enter a valid duration in seconds.";
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
            await client.BanIpAsync(new ApiClient.BanRequest
            {
                Ip = ManualIp.Trim(),
                DurationSeconds = seconds,
                Permanent = ManualPermanent,
                Reason = ManualReason
            });

            await RefreshAsync();
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to ban IP: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task UnbanAsync(string ip)
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
            await client.UnbanIpAsync(ip);

            await RefreshAsync();
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to unban IP: {ex.Message}";
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
