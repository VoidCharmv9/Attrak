using ScannerMaui.Pages;
using ScannerMaui.Services;

namespace ScannerMaui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        public static async Task NavigateToCameraPage()
        {
            try
            {
                var cameraPage = new NativeQRScannerPage();
                await Application.Current!.MainPage!.Navigation.PushAsync(cameraPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to camera: {ex.Message}");
            }
        }
    }
}
