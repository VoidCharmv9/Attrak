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
                System.Diagnostics.Debug.WriteLine("🚀 Setting up permissions on app startup...");
                
                // Use the enhanced auto-request method
                var granted = await PermissionService.AutoRequestPermissionsOnAppStart();
                
                if (granted)
                {
                    System.Diagnostics.Debug.WriteLine("🎉 All permissions granted automatically on startup!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Some permissions denied on startup - user may need to grant manually");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting up permissions on startup: {ex.Message}");
            }
        }
    }
}
