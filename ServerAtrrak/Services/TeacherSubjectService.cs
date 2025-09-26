using AttrackSharedClass.Models;
using MySql.Data.MySqlClient;
using ServerAtrrak.Data;

namespace ServerAtrrak.Services
{
    public class TeacherSubjectService
    {
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<TeacherSubjectService> _logger;

        public TeacherSubjectService(Dbconnection dbConnection, ILogger<TeacherSubjectService> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<List<TeacherSubjectAssignment>> GetTeacherSubjectsAsync(string teacherId)
        {
            var assignments = new List<TeacherSubjectAssignment>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ts.TeacherSubjectId,
                        ts.TeacherId,
                        ts.SubjectId,
                        s.SubjectName,
                        s.GradeLevel,
                        s.Strand,
                        ts.Section,
                        TIME_FORMAT(s.ScheduleStart, '%H:%i:%s') as ScheduleStart,
                        TIME_FORMAT(s.ScheduleEnd, '%H:%i:%s') as ScheduleEnd
                    FROM TeacherSubject ts
                    INNER JOIN Subject s ON ts.SubjectId = s.SubjectId
                    WHERE ts.TeacherId = @TeacherId
                    ORDER BY s.GradeLevel, s.Strand, s.ScheduleStart";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    assignments.Add(new TeacherSubjectAssignment
                    {
                        TeacherSubjectId = reader.GetString(0),
                        TeacherId = reader.GetString(1),
                        SubjectId = reader.GetString(2),
                        SubjectName = reader.GetString(3),
                        GradeLevel = reader.GetInt32(4),
                        Strand = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Section = reader.IsDBNull(6) ? null : reader.GetString(6),
                        ScheduleStart = TimeSpan.Parse(reader.GetString(7)),
                        ScheduleEnd = TimeSpan.Parse(reader.GetString(8)),
                        CreatedAt = DateTime.Now // Set default value since not in database
                    });
                }

                _logger.LogInformation("Retrieved {Count} subjects for teacher {TeacherId}", assignments.Count, teacherId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher subjects for teacher {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
            }

            return assignments;
        }

        public async Task<List<TeacherSubjectAssignment>> GetAvailableSubjectsAsync(SubjectFilter filter)
        {
            var subjects = new List<TeacherSubjectAssignment>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if Subject table is empty and initialize with sample data
                await InitializeSampleSubjectsIfEmpty(connection);

                var query = @"
                    SELECT 
                        s.SubjectId,
                        s.SubjectName,
                        s.GradeLevel,
                        s.Strand,
                        TIME_FORMAT(s.ScheduleStart, '%H:%i:%s') as ScheduleStart,
                        TIME_FORMAT(s.ScheduleEnd, '%H:%i:%s') as ScheduleEnd
                    FROM Subject s
                    WHERE s.SubjectId NOT IN (
                        SELECT DISTINCT ts.SubjectId 
                        FROM TeacherSubject ts
                    )";

                var parameters = new List<MySqlParameter>();

                if (filter.GradeLevel.HasValue)
                {
                    query += " AND s.GradeLevel = @GradeLevel";
                    parameters.Add(new MySqlParameter("@GradeLevel", filter.GradeLevel.Value));
                }

                if (!string.IsNullOrEmpty(filter.Strand))
                {
                    query += " AND s.Strand = @Strand";
                    parameters.Add(new MySqlParameter("@Strand", filter.Strand));
                }

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query += " AND s.SubjectName LIKE @SearchTerm";
                    parameters.Add(new MySqlParameter("@SearchTerm", $"%{filter.SearchTerm}%"));
                }

                query += " ORDER BY s.GradeLevel, s.Strand, s.ScheduleStart";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    subjects.Add(new TeacherSubjectAssignment
                    {
                        SubjectId = reader.GetString(0),
                        SubjectName = reader.GetString(1),
                        GradeLevel = reader.GetInt32(2),
                        Strand = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ScheduleStart = TimeSpan.Parse(reader.GetString(4)),
                        ScheduleEnd = TimeSpan.Parse(reader.GetString(5))
                    });
                }

                _logger.LogInformation("Retrieved {Count} unassigned subjects with filter: Grade={Grade}, Strand={Strand}, Search={Search}", 
                    subjects.Count, filter.GradeLevel, filter.Strand, filter.SearchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available subjects: {ErrorMessage}", ex.Message);
            }

            return subjects;
        }

        public async Task<TeacherSubjectResponse> AssignSubjectAsync(TeacherSubjectRequest request)
        {
            try
            {
                _logger.LogInformation("Assigning subject - TeacherId: {TeacherId}, SubjectId: {SubjectId}, Section: {Section}", 
                    request.TeacherId, request.SubjectId, request.Section);

                // Validate required fields
                if (string.IsNullOrEmpty(request.TeacherId))
                {
                    return new TeacherSubjectResponse { Success = false, Message = "TeacherId is required" };
                }
                if (string.IsNullOrEmpty(request.SubjectId))
                {
                    return new TeacherSubjectResponse { Success = false, Message = "SubjectId is required" };
                }

                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if teacher already has this subject
                var checkQuery = "SELECT COUNT(*) FROM TeacherSubject WHERE TeacherId = @TeacherId AND SubjectId = @SubjectId";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                checkCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);

                var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (existingCount > 0)
                {
                    return new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Teacher already has this subject assigned"
                    };
                }

                // Try to insert with Section column first, fallback to without if it doesn't exist
                try
                {
                    var insertQuery = @"
                        INSERT INTO TeacherSubject (TeacherSubjectId, TeacherId, SubjectId, Section)
                        VALUES (@TeacherSubjectId, @TeacherId, @SubjectId, @Section)";

                    using var insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@TeacherSubjectId", Guid.NewGuid().ToString());
                    insertCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                    insertCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);
                    insertCommand.Parameters.AddWithValue("@Section", request.Section ?? "");

                    await insertCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Successfully assigned subject with section: {Section}", request.Section);
                }
                catch (MySqlException ex) when (ex.Number == 1054) // Column doesn't exist
                {
                    _logger.LogWarning("Section column doesn't exist, inserting without section. Please add Section column to TeacherSubject table.");
                    
                    // Fallback: insert without Section column
                    var fallbackQuery = @"
                        INSERT INTO TeacherSubject (TeacherSubjectId, TeacherId, SubjectId)
                        VALUES (@TeacherSubjectId, @TeacherId, @SubjectId)";

                    using var fallbackCommand = new MySqlCommand(fallbackQuery, connection);
                    fallbackCommand.Parameters.AddWithValue("@TeacherSubjectId", Guid.NewGuid().ToString());
                    fallbackCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                    fallbackCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);

                    await fallbackCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Successfully assigned subject without section column");
                }

                // Update subject schedule if provided
                if (request.ScheduleStart != TimeSpan.Zero && request.ScheduleEnd != TimeSpan.Zero)
                {
                    var updateQuery = @"
                        UPDATE Subject 
                        SET ScheduleStart = @ScheduleStart, ScheduleEnd = @ScheduleEnd 
                        WHERE SubjectId = @SubjectId";

                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@ScheduleStart", request.ScheduleStart);
                    updateCommand.Parameters.AddWithValue("@ScheduleEnd", request.ScheduleEnd);
                    updateCommand.Parameters.AddWithValue("@SubjectId", request.SubjectId);

                    await updateCommand.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("Successfully assigned subject {SubjectId} to teacher {TeacherId}", request.SubjectId, request.TeacherId);

                return new TeacherSubjectResponse
                {
                    Success = true,
                    Message = "Subject assigned successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subject to teacher: {ErrorMessage}", ex.Message);
                return new TeacherSubjectResponse
                {
                    Success = false,
                    Message = $"Error assigning subject: {ex.Message}"
                };
            }
        }

        public async Task<TeacherSubjectResponse> UpdateSubjectScheduleAsync(string teacherSubjectId, TimeSpan scheduleStart, TimeSpan scheduleEnd)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    UPDATE Subject s
                    INNER JOIN TeacherSubject ts ON s.SubjectId = ts.SubjectId
                    SET s.ScheduleStart = @ScheduleStart, s.ScheduleEnd = @ScheduleEnd
                    WHERE ts.TeacherSubjectId = @TeacherSubjectId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ScheduleStart", scheduleStart);
                command.Parameters.AddWithValue("@ScheduleEnd", scheduleEnd);
                command.Parameters.AddWithValue("@TeacherSubjectId", teacherSubjectId);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully updated schedule for teacher subject {TeacherSubjectId}", teacherSubjectId);
                    return new TeacherSubjectResponse
                    {
                        Success = true,
                        Message = "Schedule updated successfully"
                    };
                }
                else
                {
                    return new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Teacher subject not found"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject schedule: {ErrorMessage}", ex.Message);
                return new TeacherSubjectResponse
                {
                    Success = false,
                    Message = $"Error updating schedule: {ex.Message}"
                };
            }
        }

        public async Task<TeacherSubjectResponse> RemoveSubjectAsync(string teacherSubjectId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = "DELETE FROM TeacherSubject WHERE TeacherSubjectId = @TeacherSubjectId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherSubjectId", teacherSubjectId);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Successfully removed teacher subject {TeacherSubjectId}", teacherSubjectId);
                    return new TeacherSubjectResponse
                    {
                        Success = true,
                        Message = "Subject removed successfully"
                    };
                }
                else
                {
                    return new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Teacher subject not found"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing teacher subject: {ErrorMessage}", ex.Message);
                return new TeacherSubjectResponse
                {
                    Success = false,
                    Message = $"Error removing subject: {ex.Message}"
                };
            }
        }

        public async Task<List<int>> GetAvailableGradesAsync()
        {
            var grades = new List<int>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if Subject table is empty and initialize with sample data
                await InitializeSampleSubjectsIfEmpty(connection);

                var query = "SELECT DISTINCT GradeLevel FROM Subject ORDER BY GradeLevel";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    grades.Add(reader.GetInt32(0));
                }

                _logger.LogInformation("Retrieved {Count} available grades", grades.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available grades: {ErrorMessage}", ex.Message);
            }

            return grades;
        }

        public async Task<List<string>> GetAvailableStrandsAsync()
        {
            var strands = new List<string>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if Subject table is empty and initialize with sample data
                await InitializeSampleSubjectsIfEmpty(connection);

                var query = "SELECT DISTINCT Strand FROM Subject WHERE Strand IS NOT NULL ORDER BY Strand";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    strands.Add(reader.GetString(0));
                }

                _logger.LogInformation("Retrieved {Count} available strands", strands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available strands: {ErrorMessage}", ex.Message);
            }

            return strands;
        }

        public async Task<List<TeacherInfo>> GetTeachersAsync()
        {
            var teachers = new List<TeacherInfo>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT t.TeacherId, t.FullName, t.Email, s.SchoolName, s.SchoolId
                    FROM Teacher t
                    INNER JOIN School s ON t.SchoolId = s.SchoolId
                    ORDER BY t.FullName";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    teachers.Add(new TeacherInfo
                    {
                        TeacherId = reader.GetString(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        SchoolName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        SchoolId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    });
                }

                _logger.LogInformation("Retrieved {Count} teachers", teachers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teachers: {ErrorMessage}", ex.Message);
            }

            return teachers;
        }

        public async Task<TeacherInfo?> GetTeacherByIdAsync(string teacherId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT t.TeacherId, t.FullName, t.Email, s.SchoolName, s.SchoolId
                    FROM Teacher t
                    INNER JOIN School s ON t.SchoolId = s.SchoolId
                    INNER JOIN User u ON t.TeacherId = u.TeacherId
                    WHERE t.TeacherId = @TeacherId AND u.UserType = 'Teacher' AND u.IsActive = TRUE";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TeacherId", teacherId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var teacher = new TeacherInfo
                    {
                        TeacherId = reader.GetString(0),
                        FullName = reader.GetString(1),
                        Email = reader.GetString(2),
                        SchoolName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        SchoolId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    };

                    _logger.LogInformation("Retrieved teacher info for {TeacherId}", teacherId);
                    return teacher;
                }

                _logger.LogWarning("Teacher not found: {TeacherId}", teacherId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher by ID {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
                return null;
            }
        }

        public async Task<TeacherSubjectResponse> AddSubjectAsync(NewSubjectRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if subject with same name and grade already exists
                var checkQuery = "SELECT COUNT(*) FROM Subject WHERE SubjectName = @SubjectName AND GradeLevel = @GradeLevel AND (Strand = @Strand OR (Strand IS NULL AND @Strand IS NULL))";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@SubjectName", request.SubjectName);
                checkCommand.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                checkCommand.Parameters.AddWithValue("@Strand", request.Strand ?? (object)DBNull.Value);

                var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (existingCount > 0)
                {
                    return new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "A subject with this name already exists for the selected grade and strand"
                    };
                }

                // Validate that end time is after start time
                if (request.ScheduleEnd <= request.ScheduleStart)
                {
                    return new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "End time must be after start time"
                    };
                }

                // Insert new subject
                var insertQuery = @"
                    INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd)
                    VALUES (@SubjectId, @SubjectName, @GradeLevel, @Strand, @ScheduleStart, @ScheduleEnd)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@SubjectId", Guid.NewGuid().ToString());
                insertCommand.Parameters.AddWithValue("@SubjectName", request.SubjectName);
                insertCommand.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
                insertCommand.Parameters.AddWithValue("@Strand", request.Strand ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ScheduleStart", request.ScheduleStart);
                insertCommand.Parameters.AddWithValue("@ScheduleEnd", request.ScheduleEnd);

                await insertCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Successfully added new subject: {SubjectName} for Grade {GradeLevel}", request.SubjectName, request.GradeLevel);

                return new TeacherSubjectResponse
                {
                    Success = true,
                    Message = "Subject added successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new subject: {ErrorMessage}", ex.Message);
                return new TeacherSubjectResponse
                {
                    Success = false,
                    Message = $"Error adding subject: {ex.Message}"
                };
            }
        }

        public async Task<List<StudentSubjectInfo>> GetStudentSubjectsAsync(string studentId)
        {
            var subjects = new List<StudentSubjectInfo>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if Subject table is empty and initialize with sample data
                await InitializeSampleSubjectsIfEmpty(connection);

                // First, get student information to determine grade level and strand
                var studentQuery = @"
                    SELECT s.GradeLevel, s.Strand 
                    FROM Student s 
                    WHERE s.StudentId = @StudentId";

                int studentGradeLevel = 0;
                string? studentStrand = null;

                using (var studentCommand = new MySqlCommand(studentQuery, connection))
                {
                    studentCommand.Parameters.AddWithValue("@StudentId", studentId);
                    using var studentReader = await studentCommand.ExecuteReaderAsync();
                    
                    if (await studentReader.ReadAsync())
                    {
                        studentGradeLevel = studentReader.GetInt32(0);
                        studentStrand = studentReader.IsDBNull(1) ? null : studentReader.GetString(1);
                    }
                    else
                    {
                        _logger.LogWarning("Student not found: {StudentId}", studentId);
                        return subjects;
                    }
                }

                // Get subjects for the student's grade level and strand that have assigned teachers
                var subjectsQuery = @"
                    SELECT 
                        s.SubjectId,
                        s.SubjectName,
                        s.GradeLevel,
                        s.Strand,
                        TIME_FORMAT(s.ScheduleStart, '%H:%i:%s') as ScheduleStart,
                        TIME_FORMAT(s.ScheduleEnd, '%H:%i:%s') as ScheduleEnd,
                        t.FullName as TeacherName,
                        t.TeacherId,
                        1 as HasTeacher
                    FROM Subject s
                    INNER JOIN TeacherSubject ts ON s.SubjectId = ts.SubjectId
                    INNER JOIN Teacher t ON ts.TeacherId = t.TeacherId
                    WHERE s.GradeLevel = @GradeLevel
                    AND (s.Strand = @Strand OR (s.Strand IS NULL AND @Strand IS NULL))
                    ORDER BY s.ScheduleStart, s.SubjectName";

                using var command = new MySqlCommand(subjectsQuery, connection);
                command.Parameters.AddWithValue("@GradeLevel", studentGradeLevel);
                command.Parameters.AddWithValue("@Strand", studentStrand ?? (object)DBNull.Value);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    subjects.Add(new StudentSubjectInfo
                    {
                        SubjectId = reader.GetString(0),
                        SubjectName = reader.GetString(1),
                        GradeLevel = reader.GetInt32(2),
                        Strand = reader.IsDBNull(3) ? null : reader.GetString(3),
                        ScheduleStart = TimeSpan.Parse(reader.GetString(4)),
                        ScheduleEnd = TimeSpan.Parse(reader.GetString(5)),
                        TeacherName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        TeacherId = reader.IsDBNull(7) ? null : reader.GetString(7),
                        HasTeacher = reader.GetInt32(8) == 1
                    });
                }

                _logger.LogInformation("Retrieved {Count} subjects for student {StudentId} (Grade {Grade}, Strand {Strand})", 
                    subjects.Count, studentId, studentGradeLevel, studentStrand ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student subjects for student {StudentId}: {ErrorMessage}", studentId, ex.Message);
            }

            return subjects;
        }

        public async Task<List<SubjectSectionInfo>> GetSubjectSectionsAsync(string subjectId)
        {
            var sections = new List<SubjectSectionInfo>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Get sections for the subject based on grade level
                var query = @"
                    SELECT 
                        st.Section as SectionId,
                        st.Section as SectionName,
                        st.GradeLevel,
                        COUNT(st.StudentId) as StudentCount,
                        @SubjectId as SubjectId
                    FROM Student st
                    INNER JOIN Subject sub ON st.GradeLevel = sub.GradeLevel
                    WHERE sub.SubjectId = @SubjectId
                    GROUP BY st.Section, st.GradeLevel
                    ORDER BY st.Section";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubjectId", subjectId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sections.Add(new SubjectSectionInfo
                    {
                        SectionId = reader.GetString(0),
                        SectionName = reader.GetString(1),
                        GradeLevel = reader.GetInt32(2),
                        StudentCount = reader.GetInt32(3),
                        SubjectId = reader.GetString(4)
                    });
                }

                _logger.LogInformation("Retrieved {Count} sections for subject {SubjectId}", sections.Count, subjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sections for subject {SubjectId}: {ErrorMessage}", subjectId, ex.Message);
            }

            return sections;
        }

        public async Task<List<SubjectSectionInfo>> GetAvailableSectionsForAssignmentAsync(string subjectId, string schoolName)
        {
            var sections = new List<SubjectSectionInfo>();

            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                _logger.LogInformation("Getting sections for SubjectId: {SubjectId}, SchoolName: {SchoolName}", subjectId, schoolName);

                // First, let's check what schools and students exist
                var debugQuery = @"
                    SELECT DISTINCT s.SchoolName, st.GradeLevel, st.Section, COUNT(st.StudentId) as StudentCount
                    FROM Student st
                    INNER JOIN School s ON st.SchoolId = s.SchoolId
                    GROUP BY s.SchoolName, st.GradeLevel, st.Section
                    ORDER BY s.SchoolName, st.GradeLevel, st.Section";

                using var debugCommand = new MySqlCommand(debugQuery, connection);
                using var debugReader = await debugCommand.ExecuteReaderAsync();
                
                _logger.LogInformation("Available schools and sections:");
                while (await debugReader.ReadAsync())
                {
                    _logger.LogInformation("School: {SchoolName}, Grade: {GradeLevel}, Section: {Section}, Students: {StudentCount}", 
                        debugReader.GetString(0), debugReader.GetInt32(1), debugReader.GetString(2), debugReader.GetInt32(3));
                }
                debugReader.Close();

                // Get ALL sections for the school - let the teacher choose which grade level
                var query = @"
                    SELECT 
                        st.Section as SectionId,
                        st.Section as SectionName,
                        st.GradeLevel,
                        COUNT(st.StudentId) as StudentCount,
                        @SubjectId as SubjectId
                    FROM Student st
                    INNER JOIN School s ON st.SchoolId = s.SchoolId
                    WHERE s.SchoolName = @SchoolName
                    GROUP BY st.Section, st.GradeLevel
                    ORDER BY st.GradeLevel, st.Section";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SubjectId", subjectId);
                command.Parameters.AddWithValue("@SchoolName", schoolName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sections.Add(new SubjectSectionInfo
                    {
                        SectionId = reader.GetString(0),
                        SectionName = reader.GetString(1),
                        GradeLevel = reader.GetInt32(2),
                        StudentCount = reader.GetInt32(3),
                        SubjectId = reader.GetString(4)
                    });
                }

                _logger.LogInformation("Retrieved {Count} available sections for subject {SubjectId} in school {SchoolName}", sections.Count, subjectId, schoolName);
                
                if (sections.Count == 0)
                {
                    _logger.LogWarning("No sections found for SubjectId: {SubjectId}, SchoolName: {SchoolName}. Check if students exist for this grade level and school.", subjectId, schoolName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available sections for subject {SubjectId} in school {SchoolName}: {ErrorMessage}", subjectId, schoolName, ex.Message);
            }

            return sections;
        }

        private async Task InitializeSampleSubjectsIfEmpty(MySqlConnection connection)
        {
            try
            {
                // Check if Subject table has any data
                var countQuery = "SELECT COUNT(*) FROM Subject";
                using var countCommand = new MySqlCommand(countQuery, connection);
                var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                if (count == 0)
                {
                    _logger.LogInformation("Subject table is empty. Initializing with sample data...");

                    // Insert sample subjects for different grades
                    var sampleSubjects = new[]
                    {
                        ("English 7", 7, null, "07:30:00", "08:30:00"),
                        ("Mathematics 7", 7, null, "08:30:00", "09:30:00"),
                        ("Science 7", 7, null, "09:30:00", "10:30:00"),
                        ("Filipino 7", 7, null, "10:30:00", "11:30:00"),
                        ("Araling Panlipunan 7", 7, null, "11:30:00", "12:30:00"),
                        ("English 8", 8, null, "07:30:00", "08:30:00"),
                        ("Mathematics 8", 8, null, "08:30:00", "09:30:00"),
                        ("Science 8", 8, null, "09:30:00", "10:30:00"),
                        ("Filipino 8", 8, null, "10:30:00", "11:30:00"),
                        ("Araling Panlipunan 8", 8, null, "11:30:00", "12:30:00"),
                        ("English 9", 9, null, "07:30:00", "08:30:00"),
                        ("Mathematics 9", 9, null, "08:30:00", "09:30:00"),
                        ("Science 9", 9, null, "09:30:00", "10:30:00"),
                        ("Filipino 9", 9, null, "10:30:00", "11:30:00"),
                        ("Araling Panlipunan 9", 9, null, "11:30:00", "12:30:00"),
                        ("English 10", 10, null, "07:30:00", "08:30:00"),
                        ("Mathematics 10", 10, null, "08:30:00", "09:30:00"),
                        ("Science 10", 10, null, "09:30:00", "10:30:00"),
                        ("Filipino 10", 10, null, "10:30:00", "11:30:00"),
                        ("Araling Panlipunan 10", 10, null, "11:30:00", "12:30:00"),
                        ("Oral Communication", 11, "ABM", "07:30:00", "08:30:00"),
                        ("General Mathematics", 11, "ABM", "08:30:00", "09:30:00"),
                        ("Earth and Life Science", 11, "ABM", "09:30:00", "10:30:00"),
                        ("Personal Development", 11, "ABM", "10:30:00", "11:30:00"),
                        ("Fundamentals of Accountancy, Business and Management 1", 11, "ABM", "11:30:00", "12:30:00"),
                        ("Oral Communication", 11, "HUMSS", "07:30:00", "08:30:00"),
                        ("General Mathematics", 11, "HUMSS", "08:30:00", "09:30:00"),
                        ("Earth and Life Science", 11, "HUMSS", "09:30:00", "10:30:00"),
                        ("Personal Development", 11, "HUMSS", "10:30:00", "11:30:00"),
                        ("Introduction to World Religions and Belief Systems", 11, "HUMSS", "11:30:00", "12:30:00"),
                        ("Oral Communication", 11, "STEM", "07:30:00", "08:30:00"),
                        ("General Mathematics", 11, "STEM", "08:30:00", "09:30:00"),
                        ("Earth and Life Science", 11, "STEM", "09:30:00", "10:30:00"),
                        ("Personal Development", 11, "STEM", "10:30:00", "11:30:00"),
                        ("Pre-Calculus", 11, "STEM", "11:30:00", "12:30:00"),
                        ("Reading and Writing Skills", 12, "ABM", "07:30:00", "08:30:00"),
                        ("Statistics and Probability", 12, "ABM", "08:30:00", "09:30:00"),
                        ("Physical Science", 12, "ABM", "09:30:00", "10:30:00"),
                        ("Fundamentals of Accountancy, Business and Management 2", 12, "ABM", "10:30:00", "11:30:00"),
                        ("Business Finance", 12, "ABM", "11:30:00", "12:30:00"),
                        ("Reading and Writing Skills", 12, "HUMSS", "07:30:00", "08:30:00"),
                        ("Statistics and Probability", 12, "HUMSS", "08:30:00", "09:30:00"),
                        ("Physical Science", 12, "HUMSS", "09:30:00", "10:30:00"),
                        ("Creative Nonfiction", 12, "HUMSS", "10:30:00", "11:30:00"),
                        ("Trends, Networks, and Critical Thinking in the 21st Century", 12, "HUMSS", "11:30:00", "12:30:00"),
                        ("Reading and Writing Skills", 12, "STEM", "07:30:00", "08:30:00"),
                        ("Statistics and Probability", 12, "STEM", "08:30:00", "09:30:00"),
                        ("Physical Science", 12, "STEM", "09:30:00", "10:30:00"),
                        ("Basic Calculus", 12, "STEM", "10:30:00", "11:30:00"),
                        ("General Physics 2", 12, "STEM", "11:30:00", "12:30:00")
                    };

                    var insertQuery = @"
                        INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd)
                        VALUES (@SubjectId, @SubjectName, @GradeLevel, @Strand, @ScheduleStart, @ScheduleEnd)";

                    foreach (var (subjectName, gradeLevel, strand, scheduleStart, scheduleEnd) in sampleSubjects)
                    {
                        using var insertCommand = new MySqlCommand(insertQuery, connection);
                        insertCommand.Parameters.AddWithValue("@SubjectId", Guid.NewGuid().ToString());
                        insertCommand.Parameters.AddWithValue("@SubjectName", subjectName);
                        insertCommand.Parameters.AddWithValue("@GradeLevel", gradeLevel);
                        insertCommand.Parameters.AddWithValue("@Strand", strand);
                        insertCommand.Parameters.AddWithValue("@ScheduleStart", scheduleStart);
                        insertCommand.Parameters.AddWithValue("@ScheduleEnd", scheduleEnd);

                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("Successfully initialized {Count} sample subjects", sampleSubjects.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing sample subjects: {ErrorMessage}", ex.Message);
            }
        }
    }
}
