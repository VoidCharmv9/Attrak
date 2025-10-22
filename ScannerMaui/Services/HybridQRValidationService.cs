using AttrackSharedClass.Models;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;

namespace ScannerMaui.Services
{
    public class HybridQRValidationService
    {
        private readonly AuthService _authService;
        private readonly OfflineDataService _offlineDataService;
        private readonly HttpClient _httpClient;
        private readonly string _serverBaseUrl;

        public HybridQRValidationService(AuthService authService, OfflineDataService offlineDataService, HttpClient httpClient)
        {
            _authService = authService;
            _offlineDataService = offlineDataService;
            _httpClient = httpClient;
            
            // Get server URL from configuration or use default
            _serverBaseUrl = "https://attrak.onrender.com/"; // Change this to your server's IP address
        }

        public async Task<QRValidationResult> ValidateQRCodeAsync(string qrCodeData)
        {
            try
            {
                var teacher = await _authService.GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "No teacher logged in. Please login first.",
                        ErrorType = QRValidationErrorType.NoTeacher
                    };
                }

                var studentData = ParseQRCodeData(qrCodeData, teacher);
                if (studentData is null)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid QR code format. Please scan a valid student QR code.",
                        ErrorType = QRValidationErrorType.InvalidFormat
                    };
                }

                // Check if online
                bool isOnline = await CheckInternetConnectionAsync();
                
                if (isOnline)
                {
                    return await ValidateOnlineAsync(studentData, teacher);
                }
                else
                {
                    return await ValidateOfflineAsync(studentData, teacher);
                }
            }
            catch (Exception ex)
            {
                return new QRValidationResult
                {
                    IsValid = false,
                    Message = $"Error validating QR code: {ex.Message}",
                    ErrorType = QRValidationErrorType.ValidationError
                };
            }
        }

        private StudentQRData? ParseQRCodeData(string qrCodeData, TeacherInfo teacher)
        {
            try
            {
                return JsonSerializer.Deserialize<StudentQRData>(qrCodeData);
            }
            catch
            {
                var parts = qrCodeData.Split('|');
                if (parts.Length >= 5)
                {
                    return new StudentQRData
                    {
                        StudentId = parts[0],
                        FullName = parts[1],
                        GradeLevel = int.TryParse(parts[2], out int grade) ? grade : 0,
                        Section = parts[3],
                        SchoolId = parts[4]
                    };
                }
                else if (parts.Length == 1)
                {
                    return new StudentQRData
                    {
                        StudentId = qrCodeData.Trim(),
                        FullName = "Unknown",
                        GradeLevel = 0,
                        Section = "Unknown",
                        SchoolId = teacher.SchoolId
                    };
                }
            }
            return null;
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverBaseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<QRValidationResult> ValidateOnlineAsync(StudentQRData studentData, TeacherInfo teacher)
        {
            try
            {
                // Call your ServerAtrrak API
                var request = new 
                { 
                    QRCodeData = JsonSerializer.Serialize(studentData), 
                    TeacherId = teacher.TeacherId 
                };
                
                var response = await _httpClient.PostAsJsonAsync($"{_serverBaseUrl}/api/qrvalidation/validate", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServerQRValidationResult>();
                    return new QRValidationResult
                    {
                        IsValid = result?.IsValid ?? false,
                        Message = result?.Message ?? "Server validation failed",
                        ErrorType = QRValidationErrorType.ValidationError,
                        StudentData = result?.StudentData
                    };
                }
                else
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "Server validation failed",
                        ErrorType = QRValidationErrorType.ValidationError
                    };
                }
            }
            catch (Exception ex)
            {
                return new QRValidationResult
                {
                    IsValid = false,
                    Message = $"Online validation error: {ex.Message}",
                    ErrorType = QRValidationErrorType.ValidationError
                };
            }
        }

        private async Task<QRValidationResult> ValidateOfflineAsync(StudentQRData studentData, TeacherInfo teacher)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== OFFLINE MODE: Saving to SQLite ===");
                System.Diagnostics.Debug.WriteLine($"Student ID: {studentData.StudentId}");
                System.Diagnostics.Debug.WriteLine($"Teacher ID: {teacher.TeacherId}");
                
                // Save to SQLite database
                var success = await _offlineDataService.SaveOfflineAttendanceAsync(
                    studentData.StudentId, 
                    "TimeIn", // Default to TimeIn for offline mode
                    teacher.TeacherId
                );
                
                System.Diagnostics.Debug.WriteLine($"SQLite save result: {success}");
                
                if (success)
                {
                    return new QRValidationResult
                    {
                        IsValid = true,
                        Message = $"Student {studentData.StudentId} saved offline (will sync when online)",
                        StudentData = studentData
                    };
                }
                else
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "Failed to save offline record",
                        ErrorType = QRValidationErrorType.ValidationError
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in offline validation: {ex.Message}");
                return new QRValidationResult
                {
                    IsValid = false,
                    Message = $"Offline save error: {ex.Message}",
                    ErrorType = QRValidationErrorType.ValidationError
                };
            }
        }

        // Sync offline data to server when online
        public async Task<bool> SyncOfflineDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Starting Offline Data Sync ===");
                
                var unsyncedRecords = await _offlineDataService.GetUnsyncedAttendanceAsync();
                System.Diagnostics.Debug.WriteLine($"Found {unsyncedRecords.Count} unsynced records");
                
                if (!unsyncedRecords.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No unsynced records to sync");
                    return true;
                }

                var teacher = await _authService.GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    System.Diagnostics.Debug.WriteLine("No teacher logged in, cannot sync");
                    return false;
                }

                // Convert offline records to server format
                var attendanceRecords = unsyncedRecords.Select(record => new
                {
                    StudentId = record.StudentId,
                    Date = record.ScanTime.Date,
                    TimeIn = record.AttendanceType == "TimeIn" ? record.ScanTime.ToString("HH:mm") : null,
                    TimeOut = record.AttendanceType == "TimeOut" ? record.ScanTime.ToString("HH:mm") : null,
                    Status = "Present",
                    Remarks = record.AttendanceType == "TimeIn" ? "Synced from offline" : "Synced from offline",
                    DeviceId = record.DeviceId
                }).ToList();

                var syncRequest = new
                {
                    TeacherId = teacher.TeacherId,
                    AttendanceRecords = attendanceRecords
                };

                System.Diagnostics.Debug.WriteLine($"Sending {attendanceRecords.Count} records to server for sync");
                
                var response = await _httpClient.PostAsJsonAsync($"{_serverBaseUrl}api/dailyattendance/sync-offline-data", syncRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    System.Diagnostics.Debug.WriteLine($"Sync successful: {result}");
                    
                    // Mark all records as synced
                    foreach (var record in unsyncedRecords)
                    {
                        await _offlineDataService.MarkAsSyncedAsync(record.Id);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Marked {unsyncedRecords.Count} records as synced");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Sync failed with status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing offline data: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendAttendanceToServerAsync(OfflineAttendanceRecord record)
        {
            try
            {
                var request = new
                {
                    StudentId = record.StudentId,
                    AttendanceType = record.AttendanceType,
                    Timestamp = record.ScanTime,
                    DeviceId = record.DeviceId
                };

                var response = await _httpClient.PostAsJsonAsync($"{_serverBaseUrl}/api/attendance/record", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ServerQRValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public StudentQRData? StudentData { get; set; }
    }

    public class QRValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public QRValidationErrorType ErrorType { get; set; }
        public StudentQRData? StudentData { get; set; }
    }

    public enum QRValidationErrorType
    {
        None,
        NoTeacher,
        StudentNotFound,
        InvalidFormat,
        SchoolMismatch,
        GradeMismatch,
        SectionMismatch,
        ValidationError
    }

    public class StudentQRData
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int GradeLevel { get; set; }
        public string Section { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string? Strand { get; set; }
    }
}
