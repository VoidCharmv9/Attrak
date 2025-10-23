using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using ScannerMaui.Services;

namespace ScannerMaui.Pages
{
    public partial class NativeQRScannerPage : ContentPage
    {
        private bool _isScanning = true;
        private CameraLocation _currentCameraLocation = CameraLocation.Rear;
        private bool _isTorchOn = false;
        private string _currentAttendanceType = string.Empty;
        private HybridQRValidationService? _qrValidationService;
        private string _lastScannedCode = string.Empty;
        private DateTime _lastScanTime = DateTime.MinValue;
        private bool _isProcessing = false;

        public event EventHandler<string>? QRCodeScanned;
        public event EventHandler<string>? AttendanceTypeSelected;

        public NativeQRScannerPage()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("NativeQRScannerPage created successfully");
        }

        public NativeQRScannerPage(HybridQRValidationService qrValidationService) : this()
        {
            _qrValidationService = qrValidationService;
        }

        private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            try
            {
                if (e.Results?.Any() == true)
                {
                    var result = e.Results.FirstOrDefault();
                    if (result != null && !string.IsNullOrEmpty(result.Value))
                    {
                        // Prevent duplicate processing of the same QR code
                        var currentTime = DateTime.Now;
                        if (result.Value == _lastScannedCode && 
                            (currentTime - _lastScanTime).TotalSeconds < 3)
                        {
                            System.Diagnostics.Debug.WriteLine($"Duplicate QR code detected, ignoring: {result.Value}");
                            return;
                        }
                        
                        // Prevent multiple simultaneous processing
                        if (_isProcessing)
                        {
                            System.Diagnostics.Debug.WriteLine("Already processing a QR code, ignoring new scan");
                            return;
                        }
                        
                        _lastScannedCode = result.Value;
                        _lastScanTime = currentTime;
                        _isProcessing = true;
                        
                        System.Diagnostics.Debug.WriteLine($"QR Code detected: {result.Value}");
                        
                        // Check if scanning is allowed at current time
                        if (!IsScanningAllowed())
                        {
                            var statusMessage = GetScanningStatusMessage();
                            
                            // Play error sound for scanning not allowed
                            PlayErrorSound();
                            
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                resultLabel.Text = "Scanning Not Allowed";
                                resultLabel.TextColor = Colors.Red;
                                resultLabel.IsVisible = true;
                                
                                statusLabel.Text = statusMessage;
                                statusLabel.TextColor = Colors.Red;
                            });
                            
                            // Clear the error message after 3 seconds
                            Task.Delay(3000).ContinueWith(_ => 
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    resultLabel.IsVisible = false;
                                    resultLabel.Text = "";
                                    statusLabel.Text = "Ready to scan next QR code";
                                    statusLabel.TextColor = Colors.Green;
                                    _isProcessing = false;
                                });
                            });
                            
                            return; // Don't process the QR code
                        }
                        
                        // Validate QR code if validation service is available
                        if (_qrValidationService != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"=== QR Code Scanned: {result.Value} ===");
                                    System.Diagnostics.Debug.WriteLine($"Attendance Type: {_currentAttendanceType}");
                                    
                                    var validationResult = await _qrValidationService.ValidateQRCodeAsync(result.Value, _currentAttendanceType);
                                    
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        try
                                        {
                                            if (validationResult.IsValid)
                                            {
                                                // Play success sound
                                                PlaySuccessSound();
                                                
                                                // Show success message
                                                resultLabel.Text = $"✓ {validationResult.Message}";
                                                resultLabel.TextColor = Colors.Green;
                                                resultLabel.IsVisible = true;
                                                
                                                statusLabel.Text = "Valid student - QR code accepted!";
                                                statusLabel.TextColor = Colors.Green;
                                                
                                                // Notify parent page about the scanned code
                                                QRCodeScanned?.Invoke(this, result.Value);
                                                
                                                // Clear the result after 3 seconds
                                                Task.Delay(3000).ContinueWith(_ => 
                                                {
                                                    MainThread.BeginInvokeOnMainThread(() =>
                                                    {
                                                        resultLabel.IsVisible = false;
                                                        resultLabel.Text = "";
                                                        statusLabel.Text = "Ready to scan next QR code";
                                                        statusLabel.TextColor = Colors.Green;
                                                        _isProcessing = false;
                                                    });
                                                });
                                            }
                                            else
                                            {
                                                // Play error sound
                                                PlayErrorSound();
                                                
                                                // Show error message
                                                resultLabel.Text = $"✗ {validationResult.Message}";
                                                resultLabel.TextColor = Colors.Red;
                                                resultLabel.IsVisible = true;
                                                
                                                statusLabel.Text = "Invalid QR code - Please try again";
                                                statusLabel.TextColor = Colors.Red;
                                                
                                                // Clear the error after 5 seconds
                                                Task.Delay(5000).ContinueWith(_ => 
                                                {
                                                    MainThread.BeginInvokeOnMainThread(() =>
                                                    {
                                                        resultLabel.IsVisible = false;
                                                        resultLabel.Text = "";
                                                        statusLabel.Text = "Ready to scan next QR code";
                                                        statusLabel.TextColor = Colors.Green;
                                                        _isProcessing = false;
                                                    });
                                                });
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error validating QR code: {ex.Message}");
                                    
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        resultLabel.Text = $"✗ Error validating QR code: {ex.Message}";
                                        resultLabel.TextColor = Colors.Red;
                                        resultLabel.IsVisible = true;
                                        
                                        statusLabel.Text = "Validation error - Please try again";
                                        statusLabel.TextColor = Colors.Red;
                                        
                                        // Reset processing flag after error
                                        Task.Delay(3000).ContinueWith(_ => 
                                        {
                                            MainThread.BeginInvokeOnMainThread(() =>
                                            {
                                                _isProcessing = false;
                                            });
                                        });
                                    });
                                }
                            });
                        }
                        else
                        {
                            // Fallback to original behavior if no validation service
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    // Show the scanned result
                                    resultLabel.Text = $"Scanned: {result.Value}";
                                    resultLabel.IsVisible = true;
                                    
                                    // Update status
                                    statusLabel.Text = "QR Code detected!";
                                    statusLabel.TextColor = Colors.Green;
                                    
                                    // Notify parent page about the scanned code
                                    QRCodeScanned?.Invoke(this, result.Value);
                                    
                                    // Show success feedback briefly
                                    resultLabel.Text = $"✓ Success: {result.Value}";
                                    resultLabel.TextColor = Colors.Green;
                                    
                                    // Clear the result after 2 seconds
                                    Task.Delay(2000).ContinueWith(_ => 
                                    {
                                        MainThread.BeginInvokeOnMainThread(() =>
                                        {
                                            resultLabel.IsVisible = false;
                                            resultLabel.Text = "";
                                            statusLabel.Text = "Ready to scan next QR code";
                                            statusLabel.TextColor = Colors.Green;
                                            _isProcessing = false;
                                        });
                                    });
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
                                }
                            });
                        }
                        
                        // Continue scanning for continuous mode
                        // Don't stop scanning - let it continue automatically
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnBarcodesDetected: {ex.Message}");
            }
        }


        private void OnTorchClicked(object? sender, EventArgs e)
        {
            _isTorchOn = !_isTorchOn;
            cameraView.IsTorchOn = _isTorchOn;
            
            torchButton.Text = _isTorchOn ? "Flashlight ON" : "Flashlight";
            torchButton.BackgroundColor = _isTorchOn ? Colors.Yellow : Colors.Orange;
            
            statusLabel.Text = _isTorchOn ? "Flashlight turned ON" : "Flashlight turned OFF";
            statusLabel.TextColor = Colors.Blue;
        }

        private void OnSwitchCameraClicked(object? sender, EventArgs e)
        {
            _currentCameraLocation = _currentCameraLocation == CameraLocation.Rear 
                ? CameraLocation.Front 
                : CameraLocation.Rear;
            
            cameraView.CameraLocation = _currentCameraLocation;
            
            var cameraType = _currentCameraLocation == CameraLocation.Rear ? "rear" : "front";
            statusLabel.Text = $"Switched to {cameraType} camera";
            statusLabel.TextColor = Colors.Blue;
        }

        public void SetAttendanceType(string attendanceType)
        {
            _currentAttendanceType = attendanceType;
            UpdateModeDisplay();
            
            statusLabel.Text = $"{(_currentAttendanceType == "TimeIn" ? "Time In" : "Time Out")} mode - Ready to scan";
            statusLabel.TextColor = Colors.Green;
        }

        private void UpdateModeDisplay()
        {
            if (!string.IsNullOrEmpty(_currentAttendanceType))
            {
                var modeText = _currentAttendanceType == "TimeIn" ? "Time In" : "Time Out";
                var icon = _currentAttendanceType == "TimeIn" ? "⏰" : "🔔";
                modeLabel.Text = $"{icon} {modeText}";
                
                // Set different colors for different modes
                modeLabel.TextColor = _currentAttendanceType == "TimeIn" ? Colors.LightBlue : Colors.LightYellow;
            }
            else
            {
                modeLabel.Text = "❓ No mode selected";
                modeLabel.TextColor = Colors.White;
            }
        }

        private async void OnDoneClicked(object? sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void PlaySuccessSound()
        {
            try
            {
                // Play vibration feedback for mobile devices
                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // Use HapticFeedback for mobile devices
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
                
                // Play actual beep sound using AudioService
                await AudioService.PlaySuccessBeepAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing success sound: {ex.Message}");
            }
        }
        
        
        private async void PlayErrorSound()
        {
            try
            {
                // Play error vibration feedback for mobile devices
                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // Use different haptic feedback for error
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                }
                
                // Play actual error beep sound using AudioService
                await AudioService.PlayErrorBeepAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing error sound: {ex.Message}");
            }
        }
        

        private bool IsScanningAllowed()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;
            
            // No scanning on weekends
            if (currentDay == DayOfWeek.Saturday || currentDay == DayOfWeek.Sunday)
            {
                return false;
            }
            
            // Define school hours
            var schoolStart = new TimeSpan(6, 0, 0);   // 6:00 AM
            var schoolEnd = new TimeSpan(18, 0, 0);    // 6:00 PM
            
            // Allow scanning only during school hours
            if (currentTime >= schoolStart && currentTime <= schoolEnd)
            {
                // Additional validation for Time In and Time Out
                if (!string.IsNullOrEmpty(_currentAttendanceType))
                {
                    if (_currentAttendanceType == "TimeIn")
                    {
                        // Time In should be in the morning (6 AM - 12 PM)
                        var morningEnd = new TimeSpan(12, 0, 0);
                        return currentTime <= morningEnd;
                    }
                    else if (_currentAttendanceType == "TimeOut")
                    {
                        // Time Out should be in the afternoon (12 PM - 6 PM)
                        var afternoonStart = new TimeSpan(12, 0, 0);
                        return currentTime >= afternoonStart;
                    }
                }
                return true;
            }
            
            return false; // Outside school hours
        }

        private string GetScanningStatusMessage()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var currentDay = DateTime.Now.DayOfWeek;
            
            // Check if it's weekend
            if (currentDay == DayOfWeek.Saturday || currentDay == DayOfWeek.Sunday)
            {
                return "Scanning not allowed: Weekends";
            }
            
            // Check if outside school hours
            var schoolStart = new TimeSpan(6, 0, 0);   // 6:00 AM
            var schoolEnd = new TimeSpan(18, 0, 0);    // 6:00 PM
            
            if (currentTime < schoolStart)
            {
                return $"Scanning not allowed: Too early (before 6:00 AM)";
            }
            else if (currentTime > schoolEnd)
            {
                return $"Scanning not allowed: Too late (after 6:00 PM)";
            }
            else
            {
                // Check Time In/Time Out specific restrictions
                if (!string.IsNullOrEmpty(_currentAttendanceType))
                {
                    if (_currentAttendanceType == "TimeIn")
                    {
                        var morningEnd = new TimeSpan(12, 0, 0);
                        if (currentTime > morningEnd)
                        {
                            return "Time In not allowed: After 12:00 PM (use Time Out instead)";
                        }
                        return "Time In allowed: Morning hours (6:00 AM - 12:00 PM)";
                    }
                    else if (_currentAttendanceType == "TimeOut")
                    {
                        var afternoonStart = new TimeSpan(12, 0, 0);
                        if (currentTime < afternoonStart)
                        {
                            return "Time Out not allowed: Before 12:00 PM (use Time In instead)";
                        }
                        return "Time Out allowed: Afternoon hours (12:00 PM - 6:00 PM)";
                    }
                }
                return "Scanning allowed: School hours (6:00 AM - 6:00 PM)";
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("NativeQRScannerPage OnAppearing called");
            
            try
            {
                // Auto-check and request permissions first
                await CheckAndRequestPermissions();
                
                // Check if scanning is allowed at current time
                if (!IsScanningAllowed())
                {
                    var statusMessage = GetScanningStatusMessage();
                    statusLabel.Text = statusMessage;
                    statusLabel.TextColor = Colors.Red;
                    
                    // Disable camera detection
                    cameraView.IsDetecting = false;
                    _isScanning = false;
                    
                    System.Diagnostics.Debug.WriteLine($"Scanning not allowed: {statusMessage}");
                    return;
                }
                
                if (!_isScanning)
                {
                    _isScanning = true;
                    cameraView.IsDetecting = true;
                    statusLabel.Text = "Camera ready - point at QR code";
                    statusLabel.TextColor = Colors.Green;
                    System.Diagnostics.Debug.WriteLine("Camera initialized and ready");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing camera: {ex.Message}");
                statusLabel.Text = "Camera error - please check permissions";
                statusLabel.TextColor = Colors.Red;
            }
        }
        
        private async Task CheckAndRequestPermissions()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Checking permissions...");
                
                // Check if all permissions are granted
                var allGranted = await PermissionService.CheckAllPermissionsAsync();
                
                if (!allGranted)
                {
                    System.Diagnostics.Debug.WriteLine("Some permissions missing, requesting...");
                    statusLabel.Text = "Requesting permissions...";
                    statusLabel.TextColor = Colors.Orange;
                    
                    // Request all required permissions
                    var granted = await PermissionService.RequestAllRequiredPermissionsAsync();
                    
                    if (granted)
                    {
                        System.Diagnostics.Debug.WriteLine("All permissions granted!");
                        statusLabel.Text = "Permissions granted - Camera ready";
                        statusLabel.TextColor = Colors.Green;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Some permissions denied");
                        statusLabel.Text = "Some permissions denied - App may not work properly";
                        statusLabel.TextColor = Colors.Red;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("All permissions already granted");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking permissions: {ex.Message}");
                statusLabel.Text = "Permission check failed";
                statusLabel.TextColor = Colors.Red;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isScanning = false;
            cameraView.IsDetecting = false;
        }
    }
}
