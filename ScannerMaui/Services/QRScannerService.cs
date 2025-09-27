using ScannerMaui.Pages;

namespace ScannerMaui.Services
{
    public class QRScannerService
    {
        public async Task OpenNativeQRScanner()
        {
            try
            {
                var scannerPage = new NativeQRScannerPage();
                
                // Subscribe to QR code detection
                scannerPage.QRCodeScanned += OnQRCodeScanned;
                
                await Application.Current!.MainPage!.Navigation.PushAsync(scannerPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening QR scanner: {ex.Message}");
            }
        }
        
        private async void OnQRCodeScanned(object? sender, string qrCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"QR Code Scanned: {qrCode}");
                
                // Navigate back to main page
                await Application.Current!.MainPage!.Navigation.PopAsync();
                
                // Here you can process the QR code for attendance
                // For now, just show a simple message
                await Application.Current!.MainPage!.DisplayAlert("QR Code Scanned", $"Student ID: {qrCode}", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
            }
        }
    }
}
