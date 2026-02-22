using RdpReaper.Gui.ViewModels;

namespace RdpReaper.Gui.Views;

public sealed partial class PolicyPage : Page
{
    public PolicyViewModel ViewModel { get; } = new();

    public PolicyPage()
    {
        InitializeComponent();
        _ = ViewModel.LoadAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAsync();
    }
}
