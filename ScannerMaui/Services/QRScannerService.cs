using ScannerMaui.Pages;

namespace ScannerMaui.Services
{
    public class QRScannerService
    {
        private readonly OfflineDataService _offlineDataService;
        private readonly HybridQRValidationService _qrValidationService;
        private string _currentAttendanceType = string.Empty;

        public event EventHandler<string>? QRCodeScanned;
        public event EventHandler<string>? OfflineDataSaved;

        public QRScannerService(OfflineDataService offlineDataService, HybridQRValidationService qrValidationService)
        {
            _offlineDataService = offlineDataService;
            _qrValidationService = qrValidationService;
        }


        public async Task OpenNativeQRScanner(string attendanceType = "")
        {
            try
            {
                _currentAttendanceType = attendanceType;
                var scannerPage = new NativeQRScannerPage(_qrValidationService);
                
                // Set the attendance type if provided
                if (!string.IsNullOrEmpty(attendanceType))
                {
                    scannerPage.SetAttendanceType(attendanceType);
                }
                
                // Subscribe to QR code detection
                scannerPage.QRCodeScanned += OnQRCodeScanned;
                
                await Application.Current!.MainPage!.Navigation.PushAsync(scannerPage);
                
                System.Diagnostics.Debug.WriteLine($"Opened native QR scanner with attendance type: {attendanceType}");
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
                
                // Process the QR code - connection status is handled by HybridQRValidationService
                await ProcessQRCode(qrCode);
                
                // Notify subscribers about the scanned QR code
                QRCodeScanned?.Invoke(this, qrCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
            }
        }

        private async Task ProcessOnlineQRCode(string qrCode)
        {
            try
            {
                // In online mode, send directly to server
                // This would typically make an API call to your server
                System.Diagnostics.Debug.WriteLine($"Processing QR code online: {qrCode}");
                
                // For now, we'll also save to offline storage as backup
                await _offlineDataService.SaveOfflineAttendanceAsync(qrCode, _currentAttendanceType);
                
                // TODO: Make API call to server
                // var success = await SendToServerAsync(qrCode, _currentAttendanceType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing online QR code: {ex.Message}");
                // Fallback to offline storage
                await ProcessOfflineQRCode(qrCode);
            }
        }

        private async Task ProcessOfflineQRCode(string qrCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Processing QR code offline: {qrCode}");
                
                // Save to offline storage
                var success = await _offlineDataService.SaveOfflineAttendanceAsync(qrCode, _currentAttendanceType);
                
                if (success)
                {
                    OfflineDataSaved?.Invoke(this, qrCode);
                    System.Diagnostics.Debug.WriteLine($"QR code saved offline: {qrCode}");
                    
                    // Try to auto-sync - connection status is handled by HybridQRValidationService
                    System.Diagnostics.Debug.WriteLine("Attempting auto-sync...");
                    await TryAutoSyncAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save QR code offline: {qrCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing offline QR code: {ex.Message}");
            }
        }

        private async Task TryAutoSyncAsync()
        {
            try
            {
                // Get API base URL from configuration or use default
                var apiBaseUrl = "https://attrak.onrender.com/"; // Your actual API URL
                
                // Get current teacher ID (you'll need to implement this)
                var teacherId = "current_teacher_id"; // Replace with actual teacher ID
                
                var syncResult = await _offlineDataService.AutoSyncOfflineDataAsync(apiBaseUrl, teacherId);
                
                if (syncResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-sync completed successfully: {syncResult.Message}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-sync completed with issues: {syncResult.Message}");
                }
                
                // Log invalid students if any
                if (syncResult.InvalidStudents?.Any() == true)
                {
                    System.Diagnostics.Debug.WriteLine($"Removed {syncResult.InvalidStudents.Count} invalid students during auto-sync");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in auto-sync: {ex.Message}");
            }
        }

        public async Task<int> GetOfflineRecordCountAsync()
        {
            return await _offlineDataService.GetUnsyncedCountAsync();
        }

        public async Task<string> ExportOfflineDataAsync()
        {
            return await _offlineDataService.ExportAttendanceDataAsync();
        }

        public async Task<bool> SaveExportToFileAsync(string fileName = null)
        {
            return await _offlineDataService.SaveExportToFileAsync(fileName);
        }

        public bool IsOnline()
        {
            // Connection status is now handled by HybridQRValidationService
            return true; // Default to true, actual status checked by HybridQRValidationService
        }

        public string GetConnectionStatus()
        {
            return "Connection status handled by HybridQRValidationService";
        }

        public async Task ProcessQRCode(string qrCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Processing QR Code: {qrCode} ===");
                
                // Use HybridQRValidationService to validate the QR code
                // This service automatically handles online/offline switching and saving
                var result = await _qrValidationService.ValidateQRCodeAsync(qrCode);
                
                if (result.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"QR code validation successful: {result.Message}");
                    OfflineDataSaved?.Invoke(this, qrCode);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"QR code validation failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public async Task<bool> SyncIndividualRecordAsync(OfflineAttendanceRecord record)
        {
            try
            {
                // Use HybridQRValidationService to sync the record
                var success = await _qrValidationService.SyncOfflineDataAsync();
                
                if (success)
                {
                    // Mark as synced in local database
                    await _offlineDataService.MarkAsSyncedAsync(record.Id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing individual record: {ex.Message}");
                return false;
            }
        }
    }
}
