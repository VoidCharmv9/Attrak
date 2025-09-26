using AttrackSharedClass.Models;
using ServerAtrrak.Data;
using MySql.Data.MySqlClient;

namespace ServerAtrrak.Services
{
    public class AttendanceService
    {
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(Dbconnection dbConnection, ILogger<AttendanceService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<AttendanceResponse> MarkAttendanceAsync(AttendanceRequest request)
        {
            try
            {
                _logger.LogInformation("Marking attendance for student {StudentId} in subject {SubjectId} with type {AttendanceType}", 
                    request.StudentId, request.SubjectId, request.AttendanceType);

                // Validate student enrollment and school/section matching
                var validationResult = await ValidateStudentEnrollmentAsync(request);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Student {StudentId} validation failed: {Message}", request.StudentId, validationResult.Message);
                    
                    return new AttendanceResponse
                    {
                        Success = true,
                        Message = validationResult.Message,
                        IsValid = false,
                        StudentName = validationResult.StudentName
                    };
                }

                // Check if attendance already marked today for this type
                var alreadyMarked = await IsAttendanceAlreadyMarkedAsync(request.StudentId, request.SubjectId, request.Timestamp.Date, request.AttendanceType);
                
                if (alreadyMarked)
                {
                    _logger.LogInformation("Attendance already marked for student {StudentId} in subject {SubjectId} for type {AttendanceType}", 
                        request.StudentId, request.SubjectId, request.AttendanceType);
                    
                    return new AttendanceResponse
                    {
                        Success = true,
                        Message = $"{request.AttendanceType} already marked for today",
                        IsValid = true,
                        StudentName = validationResult.StudentName,
                        Status = "Present",
                        AttendanceType = request.AttendanceType
                    };
                }

                // Determine status based on timing
                var status = await DetermineAttendanceStatusAsync(request);

                // Mark attendance
                await InsertAttendanceRecordAsync(request, status);

                _logger.LogInformation("Successfully marked attendance for student {StudentId} in subject {SubjectId} with status {Status}", 
                    request.StudentId, request.SubjectId, status);

                return new AttendanceResponse
                {
                    Success = true,
                    Message = $"{request.AttendanceType} marked successfully",
                    IsValid = true,
                    StudentName = validationResult.StudentName,
                    Status = status,
                    AttendanceType = request.AttendanceType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                
                return new AttendanceResponse
                {
                    Success = false,
                    Message = "An error occurred while marking attendance"
                };
            }
        }

        public async Task<List<AttendanceRecord>> GetTodayAttendanceAsync(string subjectId)
        {
            try
            {
                return await GetAttendanceForSubjectAsync(subjectId, DateTime.Today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for subject {SubjectId}: {ErrorMessage}", 
                    subjectId, ex.Message);
                return new List<AttendanceRecord>();
            }
        }

        public async Task<List<AttendanceRecord>> GetAttendanceForSubjectAsync(string subjectId, DateTime date)
        {
            var attendance = new List<AttendanceRecord>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT a.StudentId, s.FullName, a.Timestamp, a.Status
                    FROM attendance a
                    INNER JOIN Student s ON a.StudentId = s.StudentId
                    WHERE a.SubjectId = @SubjectId 
                    AND DATE(a.Timestamp) = @Date
                    ORDER BY a.Timestamp DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubjectId", subjectId);
                command.Parameters.AddWithValue("@Date", date.Date);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    attendance.Add(new AttendanceRecord
                    {
                        StudentId = reader.GetString(0),
                        StudentName = reader.GetString(1),
                        Timestamp = reader.GetDateTime(2),
                        Status = reader.GetString(3)
                    });
                }

                _logger.LogInformation("Retrieved {Count} attendance records for subject {SubjectId} on {Date}", 
                    attendance.Count, subjectId, date.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for subject {SubjectId}: {ErrorMessage}", 
                    subjectId, ex.Message);
            }

            return attendance;
        }

        private async Task<bool> IsStudentExistsAsync(string studentId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM student WHERE StudentId = @StudentId AND IsActive = TRUE";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        private async Task<bool> IsAttendanceAlreadyMarkedAsync(string studentId, string subjectId, DateTime date, string attendanceType)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            // For the current table structure, we just check if there's any attendance record for today
            // The attendance type is handled by updating the same record
            var query = @"
                SELECT COUNT(*) 
                FROM attendance 
                WHERE StudentId = @StudentId 
                AND SubjectId = @SubjectId 
                AND DATE(Timestamp) = @Date";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);
            command.Parameters.AddWithValue("@SubjectId", subjectId);
            command.Parameters.AddWithValue("@Date", date.Date);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        private async Task<string> GetStudentNameAsync(string studentId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT FullName FROM student WHERE StudentId = @StudentId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "Unknown Student";
        }

        private async Task<ValidationResult> ValidateStudentEnrollmentAsync(AttendanceRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Comprehensive validation query that checks all requirements in one go
                var validationQuery = @"
                    SELECT 
                        s.FullName as StudentName,
                        s.Section as StudentSection,
                        s.GradeLevel as StudentGradeLevel,
                        s.Strand as StudentStrand,
                        sub.SubjectName,
                        sub.GradeLevel as SubjectGradeLevel,
                        sub.Strand as SubjectStrand,
                        ts.Section as TeacherSection,
                        t.FullName as TeacherName
                    FROM student s
                    INNER JOIN StudentSubject ss ON s.StudentId = ss.StudentId
                    INNER JOIN Subject sub ON ss.SubjectId = sub.SubjectId
                    INNER JOIN TeacherSubject ts ON sub.SubjectId = ts.SubjectId
                    INNER JOIN Teacher t ON ts.TeacherId = t.TeacherId
                    WHERE s.StudentId = @StudentId 
                    AND s.SchoolId = @SchoolId
                    AND ss.SubjectId = @SubjectId
                    AND ts.TeacherId = @TeacherId
                    AND ts.SubjectId = @SubjectId";

                using var command = new MySqlCommand(validationQuery, connection);
                command.Parameters.AddWithValue("@StudentId", request.StudentId);
                command.Parameters.AddWithValue("@SchoolId", request.SchoolId);
                command.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                command.Parameters.AddWithValue("@TeacherId", request.TeacherId);

                using var reader = await command.ExecuteReaderAsync();
                
                if (!await reader.ReadAsync())
                {
                    // If no record found, check what's missing
                    reader.Close();
                    return await GetDetailedValidationError(connection, request);
                }

                var studentName = reader.GetString(0);
                var studentSection = reader.GetString(1);
                var studentGradeLevel = reader.GetInt32(2);
                var studentStrand = reader.IsDBNull(3) ? null : reader.GetString(3);
                var subjectName = reader.GetString(4);
                var subjectGradeLevel = reader.GetInt32(5);
                var subjectStrand = reader.IsDBNull(6) ? null : reader.GetString(6);
                var teacherSection = reader.IsDBNull(7) ? null : reader.GetString(7);
                var teacherName = reader.GetString(8);

                reader.Close();

                // Check if student's section matches teacher's assigned section
                if (!string.IsNullOrEmpty(teacherSection) && studentSection != teacherSection)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = $"Student is in section '{studentSection}' but teacher {teacherName} is assigned to section '{teacherSection}'",
                        StudentName = studentName
                    };
                }

                // Check if student grade matches subject grade
                if (studentGradeLevel != subjectGradeLevel)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = $"Student grade level ({studentGradeLevel}) does not match subject grade level ({subjectGradeLevel})",
                        StudentName = studentName
                    };
                }

                // For Grade 11-12, check if strand matches (if subject has a strand)
                if (subjectGradeLevel >= 11 && !string.IsNullOrEmpty(subjectStrand))
                {
                    if (string.IsNullOrEmpty(studentStrand) || studentStrand != subjectStrand)
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = $"Student strand ({studentStrand ?? "None"}) does not match subject strand ({subjectStrand})",
                            StudentName = studentName
                        };
                    }
                }

                _logger.LogInformation("Student {StudentId} ({StudentName}) validated successfully for subject {SubjectName} with teacher {TeacherName} (Grade {Grade}, Strand {Strand}, Section {Section})", 
                    request.StudentId, studentName, subjectName, teacherName, subjectGradeLevel, subjectStrand ?? "None", teacherSection ?? "Any");

                return new ValidationResult
                {
                    IsValid = true,
                    Message = "Student validation successful",
                    StudentName = studentName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating student enrollment for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Error validating student enrollment",
                    StudentName = "Unknown Student"
                };
            }
        }

        private async Task<ValidationResult> GetDetailedValidationError(MySqlConnection connection, AttendanceRequest request)
        {
            try
            {
                // Check if student exists
                var studentQuery = "SELECT FullName FROM student WHERE StudentId = @StudentId AND SchoolId = @SchoolId";
                using var studentCommand = new MySqlCommand(studentQuery, connection);
                studentCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                studentCommand.Parameters.AddWithValue("@SchoolId", request.SchoolId);
                
                var studentName = await studentCommand.ExecuteScalarAsync() as string;
                
                if (string.IsNullOrEmpty(studentName))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Student not found or not in this school",
                        StudentName = "Unknown Student"
                    };
                }

                // Check if student is enrolled in the subject
                var enrollmentQuery = "SELECT COUNT(*) FROM studentsubject WHERE StudentId = @StudentId AND SubjectId = @SubjectId";
                using var enrollmentCommand = new MySqlCommand(enrollmentQuery, connection);
                enrollmentCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                enrollmentCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                
                var enrollmentCount = Convert.ToInt32(await enrollmentCommand.ExecuteScalarAsync());
                
                if (enrollmentCount == 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Student is not enrolled in this subject",
                        StudentName = studentName
                    };
                }

                // Check if teacher is assigned to the subject
                var teacherQuery = "SELECT COUNT(*) FROM teachersubject WHERE TeacherId = @TeacherId AND SubjectId = @SubjectId";
                using var teacherCommand = new MySqlCommand(teacherQuery, connection);
                teacherCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                teacherCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                
                var teacherCount = Convert.ToInt32(await teacherCommand.ExecuteScalarAsync());
                
                if (teacherCount == 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Teacher is not assigned to this subject",
                        StudentName = studentName
                    };
                }

                // If we get here, there's some other issue
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Validation failed - please check student enrollment and teacher assignment",
                    StudentName = studentName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting detailed validation error for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Error validating student enrollment",
                    StudentName = "Unknown Student"
                };
            }
        }

        private async Task<string> DetermineAttendanceStatusAsync(AttendanceRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT ScheduleStart, ScheduleEnd
                    FROM subject
                    WHERE SubjectId = @SubjectId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubjectId", request.SubjectId);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var scheduleStart = TimeSpan.Parse(reader.GetString(0));
                    var scheduleEnd = TimeSpan.Parse(reader.GetString(1));
                    var currentTime = request.Timestamp.TimeOfDay;

                    // For Time In: Check if student is late (more than 1 minute after schedule start)
                    if (request.AttendanceType == "TimeIn")
                    {
                        if (currentTime > scheduleStart.Add(TimeSpan.FromMinutes(1)))
                        {
                            return "Late";
                        }
                        return "Present";
                    }
                    // For Time Out: Always present (early out or late out is okay)
                    else if (request.AttendanceType == "TimeOut")
                    {
                        return "Present";
                    }
                }

                return "Present"; // Default
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining attendance status for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                return "Present"; // Default to Present on error
            }
        }

        private async Task InsertAttendanceRecordAsync(AttendanceRequest request, string status)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if attendance record already exists for today
                var existingQuery = @"
                    SELECT AttendanceId FROM attendance 
                    WHERE StudentId = @StudentId 
                    AND SubjectId = @SubjectId 
                    AND DATE(Timestamp) = @Date";

                using var existingCommand = new MySqlCommand(existingQuery, connection);
                existingCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                existingCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                existingCommand.Parameters.AddWithValue("@Date", request.Timestamp.Date);

                var existingId = await existingCommand.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing record - update timestamp and status
                    var updateQuery = @"
                        UPDATE attendance 
                        SET Timestamp = @Timestamp, 
                            Status = @Status
                        WHERE AttendanceId = @AttendanceId";

                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@Timestamp", request.Timestamp);
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@AttendanceId", existingId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
                    var insertQuery = @"
                        INSERT INTO attendance (AttendanceId, StudentId, SubjectId, TeacherId, Timestamp, Status)
                        VALUES (@AttendanceId, @StudentId, @SubjectId, @TeacherId, @Timestamp, @Status)";

                    using var insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@AttendanceId", Guid.NewGuid().ToString());
                    insertCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                    insertCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                    insertCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                    insertCommand.Parameters.AddWithValue("@Timestamp", request.Timestamp);
                    insertCommand.Parameters.AddWithValue("@Status", status);

                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting attendance record for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                throw; // Re-throw to be handled by the calling method
            }
        }
    }

    public class AttendanceRequest
    {
        public string StudentId { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string AttendanceType { get; set; } = "TimeIn";
    }

    public class AttendanceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string Status { get; set; } = "Present";
        public string AttendanceType { get; set; } = "TimeIn";
    }

    public class AttendanceRecord
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "Present";
        public string AttendanceType { get; set; } = "TimeIn";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
    }
}
