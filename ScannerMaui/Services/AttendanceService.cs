using AttrackSharedClass.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ScannerMaui.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(HttpClient httpClient, ILogger<AttendanceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AttendanceResponse> MarkAttendanceAsync(AttendanceRequest request)
        {
            try
            {
                _logger.LogInformation("Marking attendance for student {StudentId} with type {AttendanceType}", 
                    request.StudentId, request.AttendanceType);

                var response = await _httpClient.PostAsJsonAsync("api/attendance/mark", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AttendanceResponse>();
                    return result ?? new AttendanceResponse 
                    { 
                        Success = false, 
                        Message = "Failed to process attendance response" 
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error marking attendance: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    return new AttendanceResponse
                    {
                        Success = false,
                        Message = $"Server error: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                
                return new AttendanceResponse
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        public async Task<List<AttendanceRecord>> GetTodayAttendanceAsync(string teacherId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/attendance/today/{teacherId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var attendance = await response.Content.ReadFromJsonAsync<List<AttendanceRecord>>();
                    return attendance ?? new List<AttendanceRecord>();
                }
                else
                {
                    _logger.LogError("Error getting today's attendance: {StatusCode}", response.StatusCode);
                    return new List<AttendanceRecord>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for teacher {TeacherId}: {ErrorMessage}", 
                    teacherId, ex.Message);
                return new List<AttendanceRecord>();
            }
        }

        public async Task<bool> ValidateStudentEnrollmentAsync(string studentId, string teacherId, string schoolId)
        {
            try
            {
                // This would typically call an API endpoint to validate student enrollment
                // For now, we'll return true as a placeholder
                // In a real implementation, you would call an API like:
                // var response = await _httpClient.GetAsync($"api/student/validate/{studentId}/{teacherId}/{schoolId}");
                // return response.IsSuccessStatusCode;
                
                _logger.LogInformation("Validating student enrollment: StudentId={StudentId}, TeacherId={TeacherId}, SchoolId={SchoolId}", 
                    studentId, teacherId, schoolId);
                
                return true; // Placeholder - implement actual validation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating student enrollment: {ErrorMessage}", ex.Message);
                return false;
            }
        }

        public async Task<Student?> GetStudentByQRCodeAsync(string qrCode)
        {
            try
            {
                // Parse QR code to extract student information
                // QR code format should be: StudentId|SchoolId|GradeLevel|Section
                var qrParts = qrCode.Split('|');
                
                if (qrParts.Length >= 4)
                {
                    var student = new Student
                    {
                        StudentId = qrParts[0],
                        SchoolId = qrParts[1],
                        GradeLevel = int.Parse(qrParts[2]),
                        Section = qrParts[3]
                    };
                    
                    _logger.LogInformation("Parsed QR code: StudentId={StudentId}, SchoolId={SchoolId}, GradeLevel={GradeLevel}, Section={Section}", 
                        student.StudentId, student.SchoolId, student.GradeLevel, student.Section);
                    
                    return student;
                }
                else
                {
                    _logger.LogWarning("Invalid QR code format: {QRCode}", qrCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing QR code {QRCode}: {ErrorMessage}", qrCode, ex.Message);
                return null;
            }
        }
    }
}
