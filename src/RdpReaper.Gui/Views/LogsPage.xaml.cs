using RdpReaper.Gui.ViewModels;

namespace RdpReaper.Gui.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; } = new();

    public LogsPage()
    {
        InitializeComponent();
        _ = ViewModel.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }
}
