using System.Runtime.InteropServices;
using System.Security.Principal;
using System.IO;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using RdpReaper.Gui.Views;

namespace RdpReaper.Gui
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window window = Window.Current;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!IsAdministrator())
            {
                ShowAdminRequiredMessage();
                Current.Exit();
                return;
            }

            window ??= new Window();

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            window.Title = "RDP Reaper";
            TrySetWindowIcon(window);

            _ = rootFrame.Navigate(typeof(ShellPage), e.Arguments);
            window.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void ShowAdminRequiredMessage()
        {
            const string title = "RdpReaper GUI";
            const string message = "Administrator privileges are required to run the GUI.";
            MessageBox(IntPtr.Zero, message, title, 0);
        }

        private static void TrySetWindowIcon(Window window)
        {
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Reaper.ico");
                if (File.Exists(iconPath))
                {
                    window.AppWindow.SetIcon(iconPath);
                }
            }
            catch
            {
                // Best-effort only.
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}
