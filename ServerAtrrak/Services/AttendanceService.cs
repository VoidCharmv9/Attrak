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
                    FROM attendace a
                    INNER JOIN student s ON a.StudentId = s.StudentId
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
                        Status = reader.GetString(3),
                        IsValid = true,
                        AttendanceType = "TimeIn",
                        Message = "Attendance recorded"
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

            var query = @"
                SELECT COUNT(*) 
                FROM attendace 
                WHERE StudentId = @StudentId 
                AND SubjectId = @SubjectId 
                AND DATE(AttendanceDate) = @Date
                AND (@AttendanceType = 'TimeIn' AND TimeIn IS NOT NULL)
                OR (@AttendanceType = 'TimeOut' AND TimeOut IS NOT NULL)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);
            command.Parameters.AddWithValue("@SubjectId", subjectId);
            command.Parameters.AddWithValue("@Date", date.Date);
            command.Parameters.AddWithValue("@AttendanceType", attendanceType);

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

            // Debug: Log the parameters
            _logger.LogInformation("Validating student: StudentId={StudentId}, SubjectId={SubjectId}, SchoolId={SchoolId}", 
                request.StudentId, request.SubjectId, request.SchoolId);
            
            // Debug: First check if student exists with basic info
            var debugStudentQuery = "SELECT StudentId, FullName, SchoolId, GradeLevel, Section FROM student WHERE StudentId = @StudentId";
            using var debugStudentCommand = new MySqlCommand(debugStudentQuery, connection);
            debugStudentCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
            
            using var debugStudentReader = await debugStudentCommand.ExecuteReaderAsync();
            if (await debugStudentReader.ReadAsync())
            {
                var debugStudentId = debugStudentReader.GetString(0);
                var debugStudentName = debugStudentReader.GetString(1);
                var debugStudentSchoolId = debugStudentReader.GetString(2);
                var debugStudentGradeLevel = debugStudentReader.GetInt32(3);
                var debugStudentSection = debugStudentReader.GetString(4);
                
                _logger.LogInformation("DEBUG - Student found: ID={StudentId}, Name={StudentName}, SchoolId={SchoolId}, GradeLevel={GradeLevel}, Section={Section}", 
                    debugStudentId, debugStudentName, debugStudentSchoolId, debugStudentGradeLevel, debugStudentSection);
            }
            else
            {
                _logger.LogWarning("DEBUG - Student NOT found in database: StudentId={StudentId}", request.StudentId);
            }
            debugStudentReader.Close();
            
            // Debug: Check subject info
            var debugSubjectQuery = "SELECT SubjectId, SubjectName, GradeLevel FROM subject WHERE SubjectId = @SubjectId";
            using var debugSubjectCommand = new MySqlCommand(debugSubjectQuery, connection);
            debugSubjectCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
            
            using var debugSubjectReader = await debugSubjectCommand.ExecuteReaderAsync();
            if (await debugSubjectReader.ReadAsync())
            {
                var debugSubjectId = debugSubjectReader.GetString(0);
                var debugSubjectName = debugSubjectReader.GetString(1);
                var debugSubjectGradeLevel = debugSubjectReader.GetInt32(2);
                
                _logger.LogInformation("DEBUG - Subject found: ID={SubjectId}, Name={SubjectName}, GradeLevel={GradeLevel}", 
                    debugSubjectId, debugSubjectName, debugSubjectGradeLevel);
            }
            else
            {
                _logger.LogWarning("DEBUG - Subject NOT found in database: SubjectId={SubjectId}", request.SubjectId);
            }
            debugSubjectReader.Close();

            var query = @"
                    SELECT s.FullName, s.SchoolId, s.Section, s.GradeLevel, sub.GradeLevel as SubjectGradeLevel
                    FROM student s
                    INNER JOIN subject sub ON s.GradeLevel = sub.GradeLevel
                    WHERE s.StudentId = @StudentId 
                    AND sub.SubjectId = @SubjectId
                    AND s.SchoolId = @SchoolId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", request.StudentId);
            command.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                command.Parameters.AddWithValue("@SchoolId", request.SchoolId);

            _logger.LogInformation("Executing validation query: {Query}", query);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var studentName = reader.GetString(0);
                    var studentSchoolId = reader.GetString(1);
                    var studentSection = reader.GetString(2);
                    var studentGradeLevel = reader.GetInt32(3);
                    var subjectGradeLevel = reader.GetInt32(4);

                    // Check if student is in the correct section (if specified)
                    if (!string.IsNullOrEmpty(request.Section) && studentSection != request.Section)
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = $"Student is not enrolled in section {request.Section}",
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

                    return new ValidationResult
                    {
                        IsValid = true,
                        Message = "Student validation successful",
                        StudentName = studentName
                    };
                }
                else
                {
                    // Debug: Check if student exists at all
                    _logger.LogWarning("No matching record found. Checking if student exists...");
                    
                    var studentExistsQuery = "SELECT FullName, SchoolId, GradeLevel FROM student WHERE StudentId = @StudentId";
                    using var studentExistsCommand = new MySqlCommand(studentExistsQuery, connection);
                    studentExistsCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                    
                    using var studentExistsReader = await studentExistsCommand.ExecuteReaderAsync();
                    if (await studentExistsReader.ReadAsync())
                    {
                        var studentName = studentExistsReader.GetString(0);
                        var studentSchoolId = studentExistsReader.GetString(1);
                        var studentGradeLevel = studentExistsReader.GetInt32(2);
                        
                        _logger.LogWarning("Student exists: Name={StudentName}, SchoolId={StudentSchoolId}, GradeLevel={StudentGradeLevel}", 
                            studentName, studentSchoolId, studentGradeLevel);
                        
                        // Check if subject exists
                        var subjectExistsQuery = "SELECT SubjectName, GradeLevel FROM subject WHERE SubjectId = @SubjectId";
                        using var subjectExistsCommand = new MySqlCommand(subjectExistsQuery, connection);
                        subjectExistsCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                        
                        using var subjectExistsReader = await subjectExistsCommand.ExecuteReaderAsync();
                        if (await subjectExistsReader.ReadAsync())
                        {
                            var subjectName = subjectExistsReader.GetString(0);
                            var subjectGradeLevel = subjectExistsReader.GetInt32(1);
                            
                            _logger.LogWarning("Subject exists: Name={SubjectName}, GradeLevel={SubjectGradeLevel}", 
                                subjectName, subjectGradeLevel);
                            
                            if (studentSchoolId != request.SchoolId)
                            {
                                return new ValidationResult
                                {
                                    IsValid = false,
                                    Message = $"Student is from different school. Student school: {studentSchoolId}, Requested school: {request.SchoolId}",
                                    StudentName = studentName
                                };
                            }
                            
                            if (studentGradeLevel != subjectGradeLevel)
                            {
                                return new ValidationResult
                                {
                                    IsValid = false,
                                    Message = $"Grade level mismatch. Student grade: {studentGradeLevel}, Subject grade: {subjectGradeLevel}",
                                    StudentName = studentName
                                };
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Subject not found: SubjectId={SubjectId}", request.SubjectId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Student not found: StudentId={StudentId}", request.StudentId);
                    }
                    
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Student not found or grade level does not match subject, or wrong school",
                        StudentName = "Unknown Student"
                    };
                }
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
                    SELECT AttendanceId FROM attendace 
                    WHERE StudentId = @StudentId 
                    AND SubjectId = @SubjectId 
                    AND AttendanceDate = @Date";

                using var existingCommand = new MySqlCommand(existingQuery, connection);
                existingCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                existingCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                existingCommand.Parameters.AddWithValue("@Date", request.Timestamp.Date);

                var existingId = await existingCommand.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing record
                    var updateQuery = @"
                        UPDATE attendace 
                        SET @TimeField = @TimeValue, 
                            STATUS = @Status
                        WHERE AttendanceId = @AttendanceId";

                    var timeField = request.AttendanceType == "TimeIn" ? "TimeIn" : "TimeOut";
                    updateQuery = updateQuery.Replace("@TimeField", timeField);

                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@TimeValue", request.Timestamp.TimeOfDay);
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@AttendanceId", existingId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
                    var insertQuery = @"
                        INSERT INTO attendace (AttendanceId, StudentId, SubjectId, AttendanceDate, TimeIn, TimeOut, STATUS)
                        VALUES (@AttendanceId, @StudentId, @SubjectId, @AttendanceDate, @TimeIn, @TimeOut, @Status)";

                    using var insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@AttendanceId", Guid.NewGuid().ToString());
                    insertCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                    insertCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                    insertCommand.Parameters.AddWithValue("@AttendanceDate", request.Timestamp.Date);
                    insertCommand.Parameters.AddWithValue("@TimeIn", request.AttendanceType == "TimeIn" ? request.Timestamp.TimeOfDay : (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("@TimeOut", request.AttendanceType == "TimeOut" ? request.Timestamp.TimeOfDay : (object)DBNull.Value);
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


    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
    }
}
