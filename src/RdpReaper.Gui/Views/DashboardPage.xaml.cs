using RdpReaper.Gui.ViewModels;

namespace RdpReaper.Gui.Views;

public sealed partial class DashboardPage : Page
{
    public StatusViewModel ViewModel { get; } = new();

    public DashboardPage()
    {
        InitializeComponent();
        _ = ViewModel.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }
}
