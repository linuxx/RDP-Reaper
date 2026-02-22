using RdpReaper.Gui.ViewModels;

namespace RdpReaper.Gui.Views;

public sealed partial class BansPage : Page
{
    public BansViewModel ViewModel { get; } = new();

    public BansPage()
    {
        InitializeComponent();
        _ = ViewModel.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void OnBanClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.BanAsync();
    }

    private async void OnUnbanClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string ip && !string.IsNullOrWhiteSpace(ip))
        {
            await ViewModel.UnbanAsync(ip);
        }
    }
}
