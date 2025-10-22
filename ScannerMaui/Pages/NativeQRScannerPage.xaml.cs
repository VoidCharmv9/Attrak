using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using AttrackSharedClass.Models;
using ScannerMaui.Services;

namespace ScannerMaui.Pages
{
    public partial class NativeQRScannerPage : ContentPage
    {
        private bool _isScanning = true;
        private CameraLocation _currentCameraLocation = CameraLocation.Rear;
        private bool _isTorchOn = false;
        private string _currentAttendanceType = string.Empty;
        private string _currentTeacherId = string.Empty;
        private string _currentSchoolId = string.Empty;
        private HashSet<string> _scannedToday = new HashSet<string>();
        private bool _isProcessingScan = false;
        private readonly IAttendanceService _attendanceService;

        public event EventHandler<string>? QRCodeScanned;
        public event EventHandler<string>? AttendanceTypeSelected;
        public event EventHandler<AttendanceResponse>? AttendanceMarked;

        public NativeQRScannerPage(IAttendanceService attendanceService)
        {
            InitializeComponent();
            _attendanceService = attendanceService;
            System.Diagnostics.Debug.WriteLine("NativeQRScannerPage created successfully");
        }

        private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            try
            {
                if (e.Results?.Any() == true && !_isProcessingScan)
                {
                    var result = e.Results.FirstOrDefault();
                    if (result != null && !string.IsNullOrEmpty(result.Value))
                    {
                        System.Diagnostics.Debug.WriteLine($"QR Code detected: {result.Value}");
                        
                        // Prevent multiple simultaneous scans
                        _isProcessingScan = true;
                        
                        // Show processing indicator
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            processingIndicator.IsVisible = true;
                            processingIndicator.IsRunning = true;
                        });
                        
                        // Check if scanning is allowed at current time
                        if (!IsScanningAllowed())
                        {
                            var statusMessage = GetScanningStatusMessage();
                            
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                resultLabel.Text = "Scanning Not Allowed";
                                resultLabel.TextColor = Colors.Red;
                                resultLabel.IsVisible = true;
                                
                                statusLabel.Text = statusMessage;
                                statusLabel.TextColor = Colors.Red;
                            });
                            
                            // Clear the error message after 3 seconds
                            await Task.Delay(3000);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                resultLabel.IsVisible = false;
                                resultLabel.Text = "";
                                statusLabel.Text = "Ready to scan next QR code";
                                statusLabel.TextColor = Colors.Green;
                                processingIndicator.IsVisible = false;
                                processingIndicator.IsRunning = false;
                                _isProcessingScan = false;
                            });
                            
                            return; // Don't process the QR code
                        }
                        
                        // Check for duplicate scan
                        var scanKey = $"{result.Value}_{_currentAttendanceType}_{DateTime.Today:yyyy-MM-dd}";
                        if (_scannedToday.Contains(scanKey))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                resultLabel.Text = "Already scanned today!";
                                resultLabel.TextColor = Colors.Orange;
                                resultLabel.IsVisible = true;
                                
                                statusLabel.Text = "This QR code was already scanned for this attendance type today";
                                statusLabel.TextColor = Colors.Orange;
                            });
                            
                            // Clear the message after 3 seconds
                            await Task.Delay(3000);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                resultLabel.IsVisible = false;
                                resultLabel.Text = "";
                                statusLabel.Text = "Ready to scan next QR code";
                                statusLabel.TextColor = Colors.Green;
                                processingIndicator.IsVisible = false;
                                processingIndicator.IsRunning = false;
                                _isProcessingScan = false;
                            });
                            
                            return;
                        }
                        
                        // Process the QR code
                        await ProcessQRCodeAsync(result.Value);
                        
                        // Mark as scanned to prevent duplicates
                        _scannedToday.Add(scanKey);
                        
                        // Reset processing flag after a delay
                        await Task.Delay(2000);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            processingIndicator.IsVisible = false;
                            processingIndicator.IsRunning = false;
                        });
                        _isProcessingScan = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnBarcodesDetected: {ex.Message}");
                _isProcessingScan = false;
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

        public void SetTeacherInfo(string teacherId, string schoolId)
        {
            _currentTeacherId = teacherId;
            _currentSchoolId = schoolId;
        }

        private async Task ProcessQRCodeAsync(string qrCode)
        {
            try
            {
                // Parse QR code to extract student information
                var student = ParseQRCode(qrCode);
                if (student == null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        resultLabel.Text = "Invalid QR Code Format";
                        resultLabel.TextColor = Colors.Red;
                        resultLabel.IsVisible = true;
                        
                        statusLabel.Text = "QR code format is invalid";
                        statusLabel.TextColor = Colors.Red;
                    });
                    return;
                }

                // Validate student enrollment (school, section, grade level matching)
                if (!await ValidateStudentEnrollmentAsync(student))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        resultLabel.Text = "Student Not Enrolled";
                        resultLabel.TextColor = Colors.Red;
                        resultLabel.IsVisible = true;
                        
                        statusLabel.Text = "Student is not enrolled in this teacher's class";
                        statusLabel.TextColor = Colors.Red;
                    });
                    return;
                }

                // Create attendance request
                var attendanceRequest = new AttendanceRequest
                {
                    StudentId = student.StudentId,
                    TeacherId = _currentTeacherId,
                    SchoolId = _currentSchoolId,
                    Section = student.Section,
                    AttendanceType = _currentAttendanceType,
                    Timestamp = DateTime.Now
                };

                // Mark attendance (this would typically call an API)
                var attendanceResponse = await MarkAttendanceAsync(attendanceRequest);

                // Update UI based on response
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (attendanceResponse.Success)
                    {
                        resultLabel.Text = $"✓ {attendanceResponse.Message}";
                        resultLabel.TextColor = Colors.Green;
                        resultLabel.IsVisible = true;
                        
                        statusLabel.Text = $"Student: {attendanceResponse.StudentName}";
                        statusLabel.TextColor = Colors.Green;
                        
                        // Notify parent page
                        AttendanceMarked?.Invoke(this, attendanceResponse);
                    }
                    else
                    {
                        resultLabel.Text = $"✗ {attendanceResponse.Message}";
                        resultLabel.TextColor = Colors.Red;
                        resultLabel.IsVisible = true;
                        
                        statusLabel.Text = "Attendance marking failed";
                        statusLabel.TextColor = Colors.Red;
                    }
                });

                // Clear the result after 3 seconds
                await Task.Delay(3000);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    resultLabel.IsVisible = false;
                    resultLabel.Text = "";
                    statusLabel.Text = "Ready to scan next QR code";
                    statusLabel.TextColor = Colors.Green;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing QR code: {ex.Message}");
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    resultLabel.Text = "Processing Error";
                    resultLabel.TextColor = Colors.Red;
                    resultLabel.IsVisible = true;
                    
                    statusLabel.Text = "Error processing QR code";
                    statusLabel.TextColor = Colors.Red;
                });
            }
        }

        private Student? ParseQRCode(string qrCode)
        {
            try
            {
                // QR code format: StudentId|SchoolId|GradeLevel|Section|FullName
                var parts = qrCode.Split('|');
                
                if (parts.Length >= 4)
                {
                    return new Student
                    {
                        StudentId = parts[0],
                        SchoolId = parts[1],
                        GradeLevel = int.Parse(parts[2]),
                        Section = parts[3],
                        FullName = parts.Length > 4 ? parts[4] : "Unknown Student"
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing QR code: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ValidateStudentEnrollmentAsync(Student student)
        {
            try
            {
                // Check if student's school matches teacher's school
                if (student.SchoolId != _currentSchoolId)
                {
                    System.Diagnostics.Debug.WriteLine($"School mismatch: Student school {student.SchoolId} vs Teacher school {_currentSchoolId}");
                    return false;
                }

                // Additional validation can be added here (e.g., check if student is in teacher's class)
                // For now, we'll return true if school matches
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating student enrollment: {ex.Message}");
                return false;
            }
        }

        private async Task<AttendanceResponse> MarkAttendanceAsync(AttendanceRequest request)
        {
            try
            {
                // Call the actual API service
                return await _attendanceService.MarkAttendanceAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking attendance: {ex.Message}");
                
                return new AttendanceResponse
                {
                    Success = false,
                    Message = "Failed to mark attendance"
                };
            }
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
            try
            {
                // Stop scanning
                _isScanning = false;
                cameraView.IsDetecting = false;
                
                // Show completion message
                statusLabel.Text = "Scanning completed";
                statusLabel.TextColor = Colors.Green;
                
                // Disable the done button to prevent multiple clicks
                doneButton.IsEnabled = false;
                doneButton.Text = "✓ Completed";
                doneButton.BackgroundColor = Colors.Gray;
                
                // Wait a moment before navigating back
                await Task.Delay(1000);
                
                // Navigate back to previous page
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnDoneClicked: {ex.Message}");
                await Navigation.PopAsync();
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("NativeQRScannerPage OnAppearing called");
            
            try
            {
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isScanning = false;
            cameraView.IsDetecting = false;
        }
    }
}
