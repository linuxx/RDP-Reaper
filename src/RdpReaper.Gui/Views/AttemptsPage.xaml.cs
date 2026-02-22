using RdpReaper.Gui.ViewModels;

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
}
