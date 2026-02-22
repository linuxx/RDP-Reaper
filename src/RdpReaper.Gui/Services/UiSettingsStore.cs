using Microsoft.Win32;

namespace RdpReaper.Gui.Services;

public static class UiSettingsStore
{
    private const string RegistryPath = @"SOFTWARE\RdpReaper\Gui";
    private const string MenuPinnedName = "MenuPinned";

    public static bool GetMenuPinned()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
        var value = key?.GetValue(MenuPinnedName);
        return value is int intValue && intValue == 1;
    }

    public static void SetMenuPinned(bool pinned)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);
        key?.SetValue(MenuPinnedName, pinned ? 1 : 0, RegistryValueKind.DWord);
    }
}
