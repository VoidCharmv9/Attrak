using AttrackSharedClass.Models;
using System.Text.Json;

namespace ScannerMaui.Services
{
    public class QRValidationService
    {
        private readonly AuthService _authService;

        public QRValidationService(AuthService authService)
        {
            _authService = authService;
        }

        public async Task<QRValidationResult> ValidateQRCodeAsync(string qrCodeData)
        {
            try
            {
                // Get current teacher information
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

                // Parse QR code data - it could be JSON, pipe-separated, or just a Student ID
                StudentQRData? studentData = null;
                try
                {
                    // First try JSON format
                    studentData = JsonSerializer.Deserialize<StudentQRData>(qrCodeData);
                }
                catch
                {
                    // If JSON parsing fails, try pipe-separated format
                    // QR code format: "StudentId|FullName|GradeLevel|Section|SchoolId"
                    var parts = qrCodeData.Split('|');
                    if (parts.Length >= 5)
                    {
                        studentData = new StudentQRData
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
                        // If it's just a single value, treat it as a Student ID
                        // This is the most common case for your QR codes
                        studentData = new StudentQRData
                        {
                            StudentId = qrCodeData.Trim(),
                            FullName = "Unknown", // Will be fetched from server
                            GradeLevel = 0,
                            Section = "Unknown",
                            SchoolId = teacher.SchoolId // Use teacher's school as default
                        };
                    }
                }

                if (studentData == null)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid QR code format. Please scan a valid student QR code.",
                        ErrorType = QRValidationErrorType.InvalidFormat
                    };
                }

                // Validate school match
                if (!string.IsNullOrEmpty(teacher.SchoolId) && 
                    !string.IsNullOrEmpty(studentData.SchoolId) && 
                    teacher.SchoolId != studentData.SchoolId)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for a different school. Student is from school ID: {studentData.SchoolId}, but you are from school ID: {teacher.SchoolId}",
                        ErrorType = QRValidationErrorType.SchoolMismatch,
                        StudentData = studentData
                    };
                }

                // Validate grade level match
                if (teacher.GradeLevel > 0 && studentData.GradeLevel > 0 && 
                    teacher.GradeLevel != studentData.GradeLevel)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for Grade {studentData.GradeLevel}, but you teach Grade {teacher.GradeLevel}",
                        ErrorType = QRValidationErrorType.GradeMismatch,
                        StudentData = studentData
                    };
                }

                // Validate section match
                if (!string.IsNullOrEmpty(teacher.Section) && 
                    !string.IsNullOrEmpty(studentData.Section) && 
                    teacher.Section != studentData.Section)
                {
                    return new QRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for section '{studentData.Section}', but you teach section '{teacher.Section}'",
                        ErrorType = QRValidationErrorType.SectionMismatch,
                        StudentData = studentData
                    };
                }

                // All validations passed
                return new QRValidationResult
                {
                    IsValid = true,
                    Message = $"Valid student: {studentData.FullName} (Grade {studentData.GradeLevel}, Section {studentData.Section})",
                    StudentData = studentData
                };
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
    }

    public class QRValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public QRValidationErrorType ErrorType { get; set; } = QRValidationErrorType.None;
        public StudentQRData? StudentData { get; set; }
    }

    public enum QRValidationErrorType
    {
        None,
        NoTeacher,
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
