using AttrackSharedClass.Models;
using ServerAtrrak.Data;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace ServerAtrrak.Services
{
    public class QRValidationService
    {
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<QRValidationService> _logger;

        public QRValidationService(Dbconnection dbConnection, ILogger<QRValidationService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<ServerQRValidationResult> ValidateQRCodeAsync(string qrCodeData, string teacherId)
        {
            try
            {
                _logger.LogInformation("Validating QR code for teacher {TeacherId}: {QRCode}", teacherId, qrCodeData);

                // Get teacher information
                var teacher = await GetTeacherInfoAsync(teacherId);
                if (teacher == null)
                {
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = "Teacher not found",
                        ErrorType = ServerQRValidationErrorType.TeacherNotFound
                    };
                }

                // Debug: Log teacher information
                _logger.LogInformation("TEACHER INFO DEBUG:");
                _logger.LogInformation("Teacher ID: {TeacherId}", teacher.TeacherId);
                _logger.LogInformation("Teacher Name: {TeacherName}", teacher.FullName);
                _logger.LogInformation("Teacher School ID: {TeacherSchoolId}", teacher.SchoolId);
                _logger.LogInformation("Teacher Grade Level: {TeacherGradeLevel}", teacher.GradeLevel);
                _logger.LogInformation("Teacher Section: '{TeacherSection}'", teacher.Section);

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
                            FullName = "Unknown", // Will be fetched from database
                            GradeLevel = 0,
                            Section = "Unknown",
                            SchoolId = teacher.SchoolId // Use teacher's school as default
                        };
                    }
                }

                if (studentData == null)
                {
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid QR code format",
                        ErrorType = ServerQRValidationErrorType.InvalidFormat
                    };
                }

                // Validate student exists and get full info
                var studentInfo = await GetQRStudentInfoAsync(studentData.StudentId);
                if (studentInfo == null)
                {
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = "Student not found in database",
                        ErrorType = ServerQRValidationErrorType.StudentNotFound,
                        StudentData = studentData
                    };
                }

                // Debug: Log student information
                _logger.LogInformation("STUDENT INFO DEBUG:");
                _logger.LogInformation("Student ID: {StudentId}", studentInfo.StudentId);
                _logger.LogInformation("Student Name: {StudentName}", studentInfo.FullName);
                _logger.LogInformation("Student School ID: {StudentSchoolId}", studentInfo.SchoolId);
                _logger.LogInformation("Student Grade Level: {StudentGradeLevel}", studentInfo.GradeLevel);
                _logger.LogInformation("Student Section: '{StudentSection}'", studentInfo.Section);

                // Validate school match
                if (teacher.SchoolId != studentInfo.SchoolId)
                {
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for a different school. Student is from school ID: {studentInfo.SchoolId}, but you are from school ID: {teacher.SchoolId}",
                        ErrorType = ServerQRValidationErrorType.SchoolMismatch,
                        StudentData = studentData
                    };
                }

                // Validate grade level match
                if (teacher.GradeLevel > 0 && studentInfo.GradeLevel > 0 && 
                    teacher.GradeLevel != studentInfo.GradeLevel)
                {
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for Grade {studentInfo.GradeLevel}, but you teach Grade {teacher.GradeLevel}",
                        ErrorType = ServerQRValidationErrorType.GradeMismatch,
                        StudentData = studentData
                    };
                }

                // Debug: Log section information
                _logger.LogInformation("=== SECTION VALIDATION DEBUG ===");
                _logger.LogInformation("Teacher Section: '{TeacherSection}' (Length: {TeacherSectionLength})", 
                    teacher.Section, teacher.Section?.Length ?? 0);
                _logger.LogInformation("Student Section: '{StudentSection}' (Length: {StudentSectionLength})", 
                    studentInfo.Section, studentInfo.Section?.Length ?? 0);
                _logger.LogInformation("Teacher Section IsNullOrEmpty: {TeacherSectionEmpty}", string.IsNullOrEmpty(teacher.Section));
                _logger.LogInformation("Student Section IsNullOrEmpty: {StudentSectionEmpty}", string.IsNullOrEmpty(studentInfo.Section));
                _logger.LogInformation("Section Comparison: '{TeacherSection}' == '{StudentSection}' = {AreEqual}", 
                    teacher.Section, studentInfo.Section, teacher.Section == studentInfo.Section);

                // Validate section match
                _logger.LogInformation("=== CHECKING SECTION VALIDATION CONDITIONS ===");
                _logger.LogInformation("Condition 1 - Teacher Section Not Empty: {TeacherSectionNotEmpty}", !string.IsNullOrEmpty(teacher.Section));
                _logger.LogInformation("Condition 2 - Student Section Not Empty: {StudentSectionNotEmpty}", !string.IsNullOrEmpty(studentInfo.Section));
                _logger.LogInformation("Condition 3 - Sections Not Equal: {SectionsNotEqual}", teacher.Section != studentInfo.Section);
                
                bool shouldValidateSection = !string.IsNullOrEmpty(teacher.Section) && 
                                           !string.IsNullOrEmpty(studentInfo.Section) && 
                                           teacher.Section != studentInfo.Section;
                
                _logger.LogInformation("Should Validate Section (All conditions met): {ShouldValidateSection}", shouldValidateSection);
                
                if (shouldValidateSection)
                {
                    _logger.LogWarning("🚫 SECTION MISMATCH DETECTED: Teacher='{TeacherSection}', Student='{StudentSection}'", 
                        teacher.Section, studentInfo.Section);
                    return new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = $"This QR code is for section '{studentInfo.Section}', but you teach section '{teacher.Section}'",
                        ErrorType = ServerQRValidationErrorType.SectionMismatch,
                        StudentData = studentData
                    };
                }
                else
                {
                    _logger.LogInformation("✅ SECTION VALIDATION PASSED: Teacher='{TeacherSection}', Student='{StudentSection}'", 
                        teacher.Section, studentInfo.Section);
                }

                // All validations passed
                return new ServerQRValidationResult
                {
                    IsValid = true,
                    Message = $"Valid student: {studentInfo.FullName} (Grade {studentInfo.GradeLevel}, Section {studentInfo.Section})",
                    StudentData = studentData,
                    QRStudentInfo = studentInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code for teacher {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
                return new ServerQRValidationResult
                {
                    IsValid = false,
                    Message = $"Error validating QR code: {ex.Message}",
                    ErrorType = ServerQRValidationErrorType.ValidationError
                };
            }
        }

        public async Task<TeacherInfo?> GetTeacherInfoAsync(string teacherId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT t.TeacherId, t.FullName, t.Email, s.SchoolName, s.SchoolId, 
                           COALESCE(t.Gradelvl, 0) as Gradelvl, COALESCE(t.Section, '') as Section, t.Strand
                    FROM teacher t
                    LEFT JOIN school s ON t.SchoolId = s.SchoolId
                    WHERE t.TeacherId = @TeacherId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new TeacherInfo
                    {
                        TeacherId = reader.GetString(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        SchoolName = reader.IsDBNull(3) ? "Unknown School" : reader.GetString(3),
                        SchoolId = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        GradeLevel = reader.GetInt32(5),
                        Section = reader.GetString(6),
                        Strand = reader.IsDBNull(7) ? null : reader.GetString(7)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher info for {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
                return null;
            }
        }

        private async Task<QRStudentInfo?> GetQRStudentInfoAsync(string studentId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT s.StudentId, s.FullName, s.GradeLevel, s.Section, s.Strand, s.SchoolId
                    FROM student s
                    WHERE s.StudentId = @StudentId AND s.IsActive = TRUE";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new QRStudentInfo
                    {
                        StudentId = reader.GetString(0),
                        FullName = reader.GetString(1),
                        GradeLevel = reader.GetInt32(2),
                        Section = reader.GetString(3),
                        Strand = reader.IsDBNull(4) ? null : reader.GetString(4),
                        SchoolId = reader.GetString(5)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student info for {StudentId}: {ErrorMessage}", studentId, ex.Message);
                return null;
            }
        }
    }

    public class ServerQRValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public ServerQRValidationErrorType ErrorType { get; set; } = ServerQRValidationErrorType.None;
        public StudentQRData? StudentData { get; set; }
        public QRStudentInfo? QRStudentInfo { get; set; }
    }

    public enum ServerQRValidationErrorType
    {
        None,
        TeacherNotFound,
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

    public class QRStudentInfo
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int GradeLevel { get; set; }
        public string Section { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string? Strand { get; set; }
    }
}
