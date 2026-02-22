using System;
using System.IO;
using System.Text.Json;
using RdpReaper.Core.Config;
using RdpReaper.Core.Security;
using RdpReaper.Gui.Services;
using RdpReaper.Gui.ViewModels;

namespace RdpReaper.Gui.Views;

public sealed partial class DashboardPage : Page
{
    public StatusViewModel ViewModel { get; } = new();
    private bool _mapReady;
    private bool _mapInitialized;

    public DashboardPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        _ = ViewModel.RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
        await LoadMapPinsAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeMap();
        await LoadMapPinsAsync();
    }

    private void InitializeMap()
    {
        if (_mapInitialized)
        {
            return;
        }

        _mapInitialized = true;
        MapView.NavigationCompleted += OnMapNavigationCompleted;
        var mapPath = Path.Combine(AppContext.BaseDirectory, "Assets", "map.html");
        MapView.Source = new Uri(mapPath);
    }

    private async void OnMapNavigationCompleted(Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
    {
        _mapReady = true;
        await LoadMapPinsAsync();
    }

    private async Task LoadMapPinsAsync()
    {
        if (!_mapReady)
        {
            return;
        }

        try
        {
            var config = ConfigStore.LoadOrCreate();
            var token = ApiSecretStore.ReadSecret();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var client = new ApiClient(config, token);
            var records = await client.GetRecentGeoAsync(200);
            var json = JsonSerializer.Serialize(records);
            await MapView.ExecuteScriptAsync($"window.setPins({json})");
        }
        catch
        {
            // Ignore map errors to avoid blocking dashboard refresh.
        }
    }
}
