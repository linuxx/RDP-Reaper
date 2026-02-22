using Microsoft.UI.Xaml.Controls;

namespace RdpReaper.Gui.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
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
}
