using ScannerMaui.Pages;

namespace ScannerMaui.Services
{
    public class NavigationService
    {
        public static async Task NavigateToCameraPage()
        {
            try
            {
                var cameraPage = new SimpleCameraPage();
                
                // Subscribe to QR code detection
                cameraPage.QRCodeScanned += OnQRCodeScanned;
                
                await Application.Current!.MainPage!.Navigation.PushAsync(cameraPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to camera page: {ex.Message}");
            }
        }

        private static void OnQRCodeScanned(object? sender, string qrCode)
        {
            System.Diagnostics.Debug.WriteLine($"QR Code Scanned: {qrCode}");
            
            // Here you can process the scanned QR code
            // For example, you could navigate back to the Blazor page with the result
            // or call an API to process the attendance
            
            // For now, just show a debug message
            Application.Current?.MainPage?.DisplayAlert("QR Code Scanned", $"Code: {qrCode}", "OK");
        }
    }
}
