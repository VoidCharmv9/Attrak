using ScannerMaui.Services;

namespace ScannerMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
            
            // Auto-setup permissions on app startup
            _ = Task.Run(async () => await SetupPermissionsOnStartup());
        }
        
        private async Task SetupPermissionsOnStartup()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Setting up permissions on app startup...");
                
                // Wait a bit for the app to fully initialize
                await Task.Delay(1000);
                
                // Check and request permissions automatically
                var granted = await PermissionService.RequestAllRequiredPermissionsAsync();
                
                if (granted)
                {
                    System.Diagnostics.Debug.WriteLine("All permissions granted on startup!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Some permissions denied on startup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up permissions on startup: {ex.Message}");
            }
        }
    }
}
