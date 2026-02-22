using Microsoft.UI.Xaml.Controls;
using RdpReaper.Gui.Services;

namespace RdpReaper.Gui.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        ApplyPaneState();
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var target = item.Tag?.ToString() switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Bans" => typeof(BansPage),
                "Attempts" => typeof(AttemptsPage),
                "Policy" => typeof(PolicyPage),
                "Logs" => typeof(LogsPage),
                _ => typeof(DashboardPage)
            };

            if (ContentFrame.CurrentSourcePageType != target)
            {
                ContentFrame.Navigate(target);
            }
        }
    }

    private void OnPinClicked(object sender, RoutedEventArgs e)
    {
        var pinned = PinToggle.IsChecked == true;
        ApplyPaneState(pinned);
        UiSettingsStore.SetMenuPinned(pinned);
    }

    private void ApplyPaneState()
    {
        var pinned = UiSettingsStore.GetMenuPinned();
        PinToggle.IsChecked = pinned;
        ApplyPaneState(pinned);
    }

    private void ApplyPaneState(bool pinned)
    {
        NavView.PaneDisplayMode = pinned ? NavigationViewPaneDisplayMode.Left : NavigationViewPaneDisplayMode.LeftMinimal;
        NavView.IsPaneOpen = pinned;
    }

}
