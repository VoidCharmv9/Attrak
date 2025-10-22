using AttrackSharedClass.Models;
using System.Text.Json;
using System.Net.Http;

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
                if (studentData == null)
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
            // For offline mode, just allow the scan and save to SQLite
            return new QRValidationResult
            {
                IsValid = true,
                Message = $"Valid student (offline mode): {studentData.StudentId}",
                StudentData = studentData
            };
        }

        // Sync offline data to server when online
        public async Task<bool> SyncOfflineDataAsync()
        {
            try
            {
                var unsyncedRecords = await _offlineDataService.GetUnsyncedAttendanceAsync();
                
                foreach (var record in unsyncedRecords)
                {
                    var success = await SendAttendanceToServerAsync(record);
                    if (success)
                    {
                        await _offlineDataService.MarkAsSyncedAsync(record.Id);
                    }
                }
                return true;
            }
            catch
            {
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
}
