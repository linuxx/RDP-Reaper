using RdpReaper.Gui.ViewModels;
using Windows.ApplicationModel.DataTransfer;

namespace RdpReaper.Gui.Views;

public sealed partial class AttemptsPage : Page
{
    public AttemptsViewModel ViewModel { get; } = new();

    public AttemptsPage()
    {
        InitializeComponent();
        _ = ViewModel.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnNextClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextPageAsync();
    }

    private async void OnPrevClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.PrevPageAsync();
    }

    private void OnCopyJsonClicked(object sender, RoutedEventArgs e)
    {
        var attempt = GetAttemptFromSender(sender);
        if (attempt != null)
        {
            var data = new DataPackage();
            data.SetText(attempt.Json);
            Clipboard.SetContent(data);
        }
    }

    private async void OnBanIpClicked(object sender, RoutedEventArgs e)
    {
        var attempt = GetAttemptFromSender(sender);
        if (attempt != null)
        {
            await ViewModel.BanIpAsync(attempt.Ip);
        }
    }

    private static AttemptView? GetAttemptFromSender(object sender)
    {
        if (sender is MenuFlyoutItem item)
        {
            if (item.Tag is AttemptView attemptTag)
            {
                return attemptTag;
            }

            if (item.DataContext is AttemptView attempt)
            {
                return attempt;
            }
        }

        return null;
    }
}
