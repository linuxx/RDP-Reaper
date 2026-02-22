using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.ViewModels;

public sealed class AttemptsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<AttemptView> Attempts { get; } = new();

    private string _filterIp = string.Empty;
    private string _filterUsername = string.Empty;
    private int _outcomeIndex;
    private DateTimeOffset? _fromDate;
    private DateTimeOffset? _toDate;
    private int _pageIndex;
    private int _pageSize = 200;
    private string _errorText = string.Empty;
    private bool _isBusy;

    public string FilterIp
    {
        get => _filterIp;
        set => SetField(ref _filterIp, value);
    }

    public string FilterUsername
    {
        get => _filterUsername;
        set => SetField(ref _filterUsername, value);
    }

    public int OutcomeIndex
    {
        get => _outcomeIndex;
        set => SetField(ref _outcomeIndex, value);
    }

    public DateTimeOffset? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public DateTimeOffset? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public string PageText => $"Page {_pageIndex + 1}";

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
            var attempts = await client.GetAttemptsAsync(new ApiClient.AttemptQuery
            {
                Take = _pageSize,
                Skip = _pageIndex * _pageSize,
                Ip = FilterIp,
                Username = FilterUsername,
                Outcome = OutcomeIndex switch
                {
                    1 => "Failure",
                    2 => "Success",
                    _ => null
                },
                From = FromDate,
                To = ToDate
            });

            Attempts.Clear();
            foreach (var attempt in attempts.Select(a => new AttemptView(a)))
            {
                Attempts.Add(attempt);
            }
        }
        catch (System.Exception ex)
        {
            ErrorText = $"Failed to load attempts: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task NextPageAsync()
    {
        _pageIndex++;
        OnPropertyChanged(nameof(PageText));
        await RefreshAsync();
    }

    public async Task PrevPageAsync()
    {
        if (_pageIndex == 0)
        {
            return;
        }

        _pageIndex--;
        OnPropertyChanged(nameof(PageText));
        await RefreshAsync();
    }

    public async Task BanIpAsync(string ip)
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
            await client.BanIpAsync(new ApiClient.BanRequest
            {
                Ip = ip,
                DurationSeconds = config.IpBanDurationSeconds,
                Permanent = false,
                Reason = "Manual ban from Attempts"
            });
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
