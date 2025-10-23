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

        public async Task<QRValidationResult> ValidateQRCodeAsync(string qrCodeData, string attendanceType = "TimeIn")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Starting QR Code Validation ===");
                System.Diagnostics.Debug.WriteLine($"QR Code Data: '{qrCodeData}'");
                System.Diagnostics.Debug.WriteLine($"Attendance Type: '{attendanceType}'");
                
                var teacher = await _authService.GetCurrentTeacherAsync();
                if (teacher == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: No teacher logged in");
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "No teacher logged in. Please login first.",
                        ErrorType = QRValidationErrorType.NoTeacher
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Teacher found: {teacher.TeacherId}, School: {teacher.SchoolId}");

                var studentData = ParseQRCodeData(qrCodeData, teacher);
                if (studentData is null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Failed to parse QR code data");
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid QR code format. Please scan a valid student QR code.",
                        ErrorType = QRValidationErrorType.InvalidFormat
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Student data parsed successfully: {studentData.StudentId}");

                // Check if online - if online, save to MySQL; if offline, save to SQLite
                bool isOnline = await CheckInternetConnectionAsync();
                
                System.Diagnostics.Debug.WriteLine($"Connection status: {(isOnline ? "ONLINE" : "OFFLINE")}");
                
                if (isOnline)
                {
                    // ONLINE MODE: Save to MySQL server
                    return await ValidateOnlineAsync(studentData, teacher, attendanceType);
                }
                else
                {
                    // OFFLINE MODE: Save to SQLite, sync later when online
                    return await ValidateOfflineAsync(studentData, teacher, attendanceType);
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
                System.Diagnostics.Debug.WriteLine($"=== Parsing QR Code Data ===");
                System.Diagnostics.Debug.WriteLine($"QR Code Data: '{qrCodeData}'");
                System.Diagnostics.Debug.WriteLine($"Teacher School ID: '{teacher.SchoolId}'");
                
                // Try JSON parsing first
                var jsonResult = JsonSerializer.Deserialize<StudentQRData>(qrCodeData);
                System.Diagnostics.Debug.WriteLine($"JSON parsing successful: {jsonResult != null}");
                return jsonResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing failed: {ex.Message}");
                
                var parts = qrCodeData.Split('|');
                System.Diagnostics.Debug.WriteLine($"Split into {parts.Length} parts: [{string.Join(", ", parts)}]");
                
                if (parts.Length >= 5)
                {
                    System.Diagnostics.Debug.WriteLine("Using pipe-separated format (5+ parts)");
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
                    System.Diagnostics.Debug.WriteLine("Using single UUID format");
                    return new StudentQRData
                    {
                        StudentId = qrCodeData.Trim(),
                        FullName = "Unknown",
                        GradeLevel = 0,
                        Section = "Unknown",
                        SchoolId = teacher.SchoolId
                    };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Unsupported format: {parts.Length} parts");
                }
            }
            return null;
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                // Fix the double slash issue
                var healthUrl = _serverBaseUrl.EndsWith("/") ? $"{_serverBaseUrl}api/health" : $"{_serverBaseUrl}/api/health";
                System.Diagnostics.Debug.WriteLine($"Health check URL: {healthUrl}");
                var response = await _httpClient.GetAsync(healthUrl);
                System.Diagnostics.Debug.WriteLine($"Health check response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Health check error: {ex.Message}");
                return false;
            }
        }

        private async Task<QRValidationResult> ValidateOnlineAsync(StudentQRData studentData, TeacherInfo teacher, string attendanceType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ONLINE MODE: Saving to MySQL ===");
                System.Diagnostics.Debug.WriteLine($"Student ID: {studentData.StudentId}");
                System.Diagnostics.Debug.WriteLine($"Teacher ID: {teacher.TeacherId}");
                System.Diagnostics.Debug.WriteLine($"Attendance Type: {attendanceType}");
                
                var currentTime = DateTime.Now;
                
                HttpResponseMessage response;
                
                if (attendanceType == "TimeIn")
                {
                    var request = new DailyTimeInRequest
                    {
                        StudentId = studentData.StudentId,
                        Date = currentTime.Date,
                        TimeIn = currentTime.TimeOfDay
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Sending Time In request to server");
                    var timeInUrl = _serverBaseUrl.EndsWith("/") ? $"{_serverBaseUrl}api/dailyattendance/daily-timein" : $"{_serverBaseUrl}/api/dailyattendance/daily-timein";
                    System.Diagnostics.Debug.WriteLine($"Full URL: {timeInUrl}");
                    response = await _httpClient.PostAsJsonAsync(timeInUrl, request);
                }
                else
                {
                    var request = new DailyTimeOutRequest
                    {
                        StudentId = studentData.StudentId,
                        Date = currentTime.Date,
                        TimeOut = currentTime.TimeOfDay
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Sending Time Out request to server");
                    var timeOutUrl = _serverBaseUrl.EndsWith("/") ? $"{_serverBaseUrl}api/dailyattendance/daily-timeout" : $"{_serverBaseUrl}/api/dailyattendance/daily-timeout";
                    System.Diagnostics.Debug.WriteLine($"Full URL: {timeOutUrl}");
                    response = await _httpClient.PostAsJsonAsync(timeOutUrl, request);
                }
                
                System.Diagnostics.Debug.WriteLine($"Server response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Server response content: {responseContent}");
                    
                    var displayType = attendanceType == "TimeIn" ? "Time In" : "Time Out";
                    return new QRValidationResult
                    {
                        IsValid = true,
                        Message = $"✓ {displayType} saved successfully to server",
                        StudentData = studentData
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Server error: {errorContent}");
                    System.Diagnostics.Debug.WriteLine($"Error status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Error reason: {response.ReasonPhrase}");
                    
                    // Check if this is a "No Time In found" error for TimeOut
                    if (attendanceType == "TimeOut" && response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            var errorResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent);
                            if (errorResponse != null && errorResponse.ContainsKey("message"))
                            {
                                var errorMessage = errorResponse["message"]?.ToString();
                                if (errorMessage != null && errorMessage.Contains("No Time In found"))
                                {
                                    System.Diagnostics.Debug.WriteLine("TimeOut called without TimeIn - returning error instead of fallback");
                                    return new QRValidationResult
                                    {
                                        IsValid = false,
                                        Message = "❌ No Time In found for today. Please mark Time In first.",
                                        StudentData = studentData
                                    };
                                }
                                else if (errorMessage != null && errorMessage.Contains("Time Out already marked"))
                                {
                                    System.Diagnostics.Debug.WriteLine("TimeOut already exists - returning error instead of fallback");
                                    return new QRValidationResult
                                    {
                                        IsValid = false,
                                        Message = "❌ Time Out already marked for today.",
                                        StudentData = studentData
                                    };
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing server response: {parseEx.Message}");
                        }
                    }
                    
                    // For other server errors, return error (don't fallback to SQLite when online)
                    System.Diagnostics.Debug.WriteLine("Server error - returning error message");
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = $"❌ Server error: {response.StatusCode} - {errorContent}",
                        StudentData = studentData
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Online validation error: {ex.Message}");
                
                // Online failed - return error (don't fallback to SQLite when online)
                System.Diagnostics.Debug.WriteLine("Online validation failed - returning error");
                return new QRValidationResult
                {
                    IsValid = false,
                    Message = $"❌ Connection error: {ex.Message}",
                    StudentData = studentData
                };
            }
        }

        private async Task<QRValidationResult> ValidateOfflineAsync(StudentQRData studentData, TeacherInfo teacher, string attendanceType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== OFFLINE MODE: Saving to SQLite ===");
                System.Diagnostics.Debug.WriteLine($"Student ID: {studentData.StudentId}");
                System.Diagnostics.Debug.WriteLine($"Teacher ID: {teacher.TeacherId}");
                System.Diagnostics.Debug.WriteLine($"Attendance Type: {attendanceType}");
                
                // Save to SQLite database
                var success = await _offlineDataService.SaveOfflineAttendanceAsync(
                    studentData.StudentId, 
                    attendanceType, // Use the provided attendance type
                    teacher.TeacherId
                );
                
                System.Diagnostics.Debug.WriteLine($"SQLite save result: {success}");
                
                if (success)
                {
                    var displayType = attendanceType == "TimeIn" ? "Time In" : "Time Out";
                    return new QRValidationResult
                    {
                        IsValid = true,
                        Message = $"✓ {displayType} saved offline (will sync when online)",
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

        private async Task<string> DetermineAttendanceTypeAsync(string studentId, string teacherId)
        {
            try
            {
                // Check if student has Time In for today
                var today = DateTime.Today;
                var statusUrl = _serverBaseUrl.EndsWith("/") ? $"{_serverBaseUrl}api/dailyattendance/daily-status/{studentId}?date={today:yyyy-MM-dd}" : $"{_serverBaseUrl}/api/dailyattendance/daily-status/{studentId}?date={today:yyyy-MM-dd}";
                var timeInResponse = await _httpClient.GetFromJsonAsync<DailyAttendanceStatus>(statusUrl);
                var hasTimeIn = timeInResponse?.TimeIn != null;
                
                // Check if student has Time Out for today
                var todayUrl = _serverBaseUrl.EndsWith("/") ? $"{_serverBaseUrl}api/dailyattendance/today/{teacherId}" : $"{_serverBaseUrl}/api/dailyattendance/today/{teacherId}";
                var todayResponse = await _httpClient.GetFromJsonAsync<List<DailyAttendanceRecord>>(todayUrl);
                var hasTimeOut = todayResponse?.Any(r => r.StudentId == studentId && !string.IsNullOrEmpty(r.TimeOut)) == true;

                System.Diagnostics.Debug.WriteLine($"Student {studentId} - HasTimeIn: {hasTimeIn}, HasTimeOut: {hasTimeOut}");

                // Auto-determine attendance type
                if (!hasTimeIn)
                {
                    return "TimeIn";
                }
                else if (!hasTimeOut)
                {
                    return "TimeOut";
                }
                else
                {
                    // Both Time In and Time Out already exist - this shouldn't happen in normal flow
                    return "TimeIn"; // Default fallback
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining attendance type: {ex.Message}");
                // Default to TimeIn if we can't determine
                return "TimeIn";
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

                // Group records by student and date to consolidate TimeIn/TimeOut
                var groupedRecords = unsyncedRecords
                    .GroupBy(r => new { r.StudentId, Date = r.ScanTime.Date })
                    .Select(group => 
                    {
                        // Get the LATEST TimeIn and TimeOut records to avoid duplicates
                        var timeInRecord = group.Where(r => r.AttendanceType == "TimeIn")
                                              .OrderByDescending(r => r.ScanTime)
                                              .FirstOrDefault();
                        var timeOutRecord = group.Where(r => r.AttendanceType == "TimeOut")
                                                .OrderByDescending(r => r.ScanTime)
                                                .FirstOrDefault();
                        
                        // Determine status and remarks based on actual attendance
                        string status = "Present";
                        string remarks = "";
                        
                        if (timeInRecord != null && timeOutRecord != null)
                        {
                            // Both TimeIn and TimeOut exist
                            var timeIn = timeInRecord.ScanTime;
                            var timeOut = timeOutRecord.ScanTime;
                            
                            // Check if it's a half day (less than 4 hours)
                            var duration = timeOut - timeIn;
                            if (duration.TotalHours < 4)
                            {
                                status = "Halfday";
                                remarks = "Half day attendance";
                            }
                            else
                            {
                                status = "Present";
                                remarks = "Full day attendance";
                            }
                        }
                        else if (timeInRecord != null)
                        {
                            // Only TimeIn exists - check if late
                            var timeIn = timeInRecord.ScanTime;
                            if (timeIn.Hour > 8 || (timeIn.Hour == 8 && timeIn.Minute > 0))
                            {
                                status = "Late";
                                remarks = "Late arrival";
                            }
                            else
                            {
                                status = "Present";
                                remarks = "On time";
                            }
                        }
                        else if (timeOutRecord != null)
                        {
                            // Only TimeOut exists (unusual case)
                            status = "Present";
                            remarks = "Time out only";
                        }
                        
                        return new
                        {
                            StudentId = group.Key.StudentId,
                            Date = group.Key.Date,
                            TimeIn = timeInRecord?.ScanTime.ToString("HH:mm"),
                            TimeOut = timeOutRecord?.ScanTime.ToString("HH:mm"),
                            Status = status,
                            Remarks = remarks,
                            DeviceId = group.First().DeviceId
                        };
                }).ToList();

                var syncRequest = new
                {
                    TeacherId = teacher.TeacherId,
                    AttendanceRecords = groupedRecords
                };

                System.Diagnostics.Debug.WriteLine($"Sending {groupedRecords.Count} consolidated records to server for sync");
                
                var response = await _httpClient.PostAsJsonAsync($"{_serverBaseUrl}api/dailyattendance/sync-offline-data", syncRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    System.Diagnostics.Debug.WriteLine($"Sync successful: {result}");
                    
                    // Mark all records as synced
                    System.Diagnostics.Debug.WriteLine($"Marking {unsyncedRecords.Count} records as synced...");
                    foreach (var record in unsyncedRecords)
                    {
                        System.Diagnostics.Debug.WriteLine($"Marking record {record.Id} as synced...");
                        var markResult = await _offlineDataService.MarkAsSyncedAsync(record.Id);
                        System.Diagnostics.Debug.WriteLine($"Mark result for record {record.Id}: {markResult}");
                        
                        // If marking failed, try alternative approach
                        if (!markResult)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to mark record {record.Id}, trying alternative approach...");
                            var alternativeResult = await _offlineDataService.MarkAsSyncedByStudentIdAsync(record.StudentId);
                            System.Diagnostics.Debug.WriteLine($"Alternative mark result for student {record.StudentId}: {alternativeResult}");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Completed marking {unsyncedRecords.Count} records as synced");
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
