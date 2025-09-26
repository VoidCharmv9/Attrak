using ScannerMaui.Pages;

namespace ScannerMaui.Services
{
    public class QRScannerService
    {
        public event EventHandler<string>? QRCodeScanned;
        
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
                
                // Trigger the event for the QRScanner page to handle
                QRCodeScanned?.Invoke(this, qrCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
            }
        }
    }
}
