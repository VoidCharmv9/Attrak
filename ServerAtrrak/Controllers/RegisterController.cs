using Microsoft.AspNetCore.Mvc;
using AttrackSharedClass.Models;
using ServerAtrrak.Services;
using MySql.Data.MySqlClient;
using ServerAtrrak.Data;
using QRCoder;
using System.Drawing.Imaging;
using System.Data;

namespace ServerAtrrak.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly SchoolService _schoolService;
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(IAuthService authService, SchoolService schoolService, Dbconnection dbConnection, ILogger<RegisterController> logger)
        {
            _authService = authService;
            _schoolService = schoolService;
            _dbConnection = dbConnection;
            _logger = logger;
        }

        [HttpPost("student")]
        public async Task<ActionResult<StudentRegisterResponse>> RegisterStudent([FromBody] StudentRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new StudentRegisterResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                // Check if username already exists
                var existingUser = await _authService.GetUserByUsernameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest(new StudentRegisterResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    });
                }

                // Check if email already exists
                if (await EmailExistsAsync(request.Email))
                {
                    return BadRequest(new StudentRegisterResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    });
                }

                // Find school by name
                var schoolId = await FindSchoolByNameAsync(request.SchoolName);
                if (string.IsNullOrEmpty(schoolId))
                {
                    return BadRequest(new StudentRegisterResponse
                    {
                        Success = false,
                        Message = "School not found. Please select a valid school from the list."
                    });
                }

                // Create student record
                var studentId = await CreateStudentAsync(request, schoolId);

                // Create user record
                var userId = await CreateUserForStudentAsync(request, studentId);

                // Generate QR code
                var qrCodeData = await GenerateQRCodeAsync(studentId);

                _logger.LogInformation("Student registered successfully: {Username}", request.Username);

                return Ok(new StudentRegisterResponse
                {
                    Success = true,
                    Message = "Student registered successfully",
                    UserId = userId,
                    StudentId = studentId,
                    QRCodeData = qrCodeData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student registration for user: {Username}. Error: {ErrorMessage}", request.Username, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new StudentRegisterResponse
                {
                    Success = false,
                    Message = $"An error occurred during registration: {ex.Message}"
                });
            }
        }

        [HttpPost("teacher")]
        public async Task<ActionResult<RegisterResponse>> RegisterTeacher([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                // Check if username already exists
                var existingUser = await _authService.GetUserByUsernameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    });
                }

                // Check if email already exists
                if (await EmailExistsAsync(request.Email))
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    });
                }

                // Check if school exists, if not create it
                var schoolId = await _schoolService.GetOrCreateSchoolAsync(request);

                // Create teacher record
                var teacherId = await CreateTeacherAsync(request, schoolId);

                // Create user record
                var userId = await CreateUserAsync(request, teacherId);

                _logger.LogInformation("Teacher registered successfully: {Username}", request.Username);

                return Ok(new RegisterResponse
                {
                    Success = true,
                    Message = "Teacher registered successfully",
                    UserId = userId,
                    TeacherId = teacherId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during teacher registration for user: {Username}", request.Username);
                return StatusCode(500, new RegisterResponse
                {
                    Success = false,
                    Message = "An error occurred during registration"
                });
            }
        }

        [HttpGet("regions")]
        public async Task<ActionResult<List<string>>> GetRegions()
        {
            try
            {
                var regions = await _schoolService.GetRegionsAsync();
                return Ok(regions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting regions: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<string>());
            }
        }

        [HttpGet("divisions/{region}")]
        public async Task<ActionResult<List<string>>> GetDivisions(string region)
        {
            try
            {
                _logger.LogInformation("API: Getting divisions for region: {Region}", region);
                var divisions = await _schoolService.GetDivisionsByRegionAsync(region);
                _logger.LogInformation("API: Returning {Count} divisions: {Divisions}", divisions.Count, string.Join(", ", divisions));
                return Ok(divisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting divisions for region: {Region}", region);
                return StatusCode(500, new List<string>());
            }
        }

        [HttpGet("districts/{division}")]
        public async Task<ActionResult<List<string>>> GetDistricts(string division)
        {
            try
            {
                var districts = await _schoolService.GetDistrictsByDivisionAsync(division);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting districts for division: {Division}", division);
                return StatusCode(500, new List<string>());
            }
        }

        [HttpGet("schools/all")]
        public async Task<ActionResult<List<SchoolInfo>>> GetAllSchools()
        {
            try
            {
                var schools = await _schoolService.GetAllSchoolsAsync();
                return Ok(schools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all schools: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<SchoolInfo>());
            }
        }

        [HttpGet("student/{studentId}/qr")]
        public async Task<ActionResult<string>> GetStudentQRCode(string studentId)
        {
            try
            {
                _logger.LogInformation("Getting QR code for student: {StudentId}", studentId);
                var qrCodeData = await GetStudentQRImageAsync(studentId);
                
                if (string.IsNullOrEmpty(qrCodeData))
                {
                    // If QR code doesn't exist, generate it
                    _logger.LogInformation("QR code not found for student {StudentId}, generating new one", studentId);
                    qrCodeData = await GenerateQRCodeAsync(studentId);
                    if (string.IsNullOrEmpty(qrCodeData))
                    {
                        _logger.LogError("Failed to generate QR code for student: {StudentId}", studentId);
                        return NotFound("Failed to generate QR code for this student");
                    }
                    _logger.LogInformation("Successfully generated QR code for student: {StudentId}, Length: {Length}", studentId, qrCodeData.Length);
                }
                else
                {
                    _logger.LogInformation("Retrieved existing QR code for student: {StudentId}, Length: {Length}", studentId, qrCodeData.Length);
                }
                
                return Ok(qrCodeData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting QR code for student: {StudentId}", studentId);
                return StatusCode(500, "Error retrieving QR code");
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<StudentInfo>> GetStudentInfo(string studentId)
        {
            try
            {
                var studentInfo = await GetStudentByIdAsync(studentId);
                if (studentInfo == null)
                {
                    return NotFound("Student not found");
                }
                return Ok(studentInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student info: {StudentId}", studentId);
                return StatusCode(500, "Error retrieving student information");
            }
        }

        [HttpGet("school/{schoolId}")]
        public async Task<ActionResult<SchoolInfo>> GetSchoolInfo(string schoolId)
        {
            try
            {
                var schoolInfo = await GetSchoolByIdAsync(schoolId);
                if (schoolInfo == null)
                {
                    return NotFound("School not found");
                }
                return Ok(schoolInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting school info: {SchoolId}", schoolId);
                return StatusCode(500, "Error retrieving school information");
            }
        }

        private async Task<bool> EmailExistsAsync(string email)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM user WHERE Email = @Email";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }


        private async Task<string> CreateTeacherAsync(RegisterRequest request, string schoolId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var teacherId = Guid.NewGuid().ToString();
            var query = @"
                INSERT INTO teacher (TeacherId, FullName, Email, SchoolId)
                VALUES (@TeacherId, @FullName, @Email, @SchoolId)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@TeacherId", teacherId);
            command.Parameters.AddWithValue("@FullName", request.FullName);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@SchoolId", schoolId);

            await command.ExecuteNonQueryAsync();
            return teacherId;
        }

        private async Task<string> CreateUserAsync(RegisterRequest request, string teacherId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var userId = Guid.NewGuid().ToString();
            var query = @"
                INSERT INTO user (UserId, Username, Email, Password, UserType, IsActive, CreatedAt, UpdatedAt, TeacherId)
                VALUES (@UserId, @Username, @Email, @Password, @UserType, @IsActive, @CreatedAt, @UpdatedAt, @TeacherId)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Username", request.Username);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@Password", request.Password);
            command.Parameters.AddWithValue("@UserType", "Teacher");
            command.Parameters.AddWithValue("@IsActive", true);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@TeacherId", teacherId);

            await command.ExecuteNonQueryAsync();
            return userId;
        }

        private async Task<string?> FindSchoolByNameAsync(string schoolName)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT SchoolId FROM school WHERE SchoolName = @SchoolName LIMIT 1";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchoolName", schoolName);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        private async Task<string> CreateStudentAsync(StudentRegisterRequest request, string schoolId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var studentId = Guid.NewGuid().ToString();
            var query = @"
                INSERT INTO student (StudentId, FullName, Email, GradeLevel, Section, Strand, SchoolId, QRImage)
                VALUES (@StudentId, @FullName, @Email, @GradeLevel, @Section, @Strand, @SchoolId, @QRImage)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);
            command.Parameters.AddWithValue("@FullName", request.FullName);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@GradeLevel", request.GradeLevel);
            command.Parameters.AddWithValue("@Section", request.Section);
            command.Parameters.AddWithValue("@Strand", request.Strand ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SchoolId", schoolId);
            command.Parameters.AddWithValue("@QRImage", ""); // Will be updated after QR generation

            await command.ExecuteNonQueryAsync();
            return studentId;
        }

        private async Task<string> CreateUserForStudentAsync(StudentRegisterRequest request, string studentId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var userId = Guid.NewGuid().ToString();
            var query = @"
                INSERT INTO user (UserId, Username, Email, Password, UserType, IsActive, CreatedAt, UpdatedAt, StudentId)
                VALUES (@UserId, @Username, @Email, @Password, @UserType, @IsActive, @CreatedAt, @UpdatedAt, @StudentId)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Username", request.Username);
            command.Parameters.AddWithValue("@Email", request.Email);
            command.Parameters.AddWithValue("@Password", request.Password);
            command.Parameters.AddWithValue("@UserType", UserType.Student.ToString());
            command.Parameters.AddWithValue("@IsActive", true);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@StudentId", studentId);

            await command.ExecuteNonQueryAsync();
            return userId;
        }

        private async Task<string> GenerateQRCodeAsync(string studentId)
        {
            try
            {
                // Generate QR code with student ID as content
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(studentId, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new Base64QRCode(qrCodeData);
                
                // Generate QR code as base64 string
                var qrCodeBase64 = qrCode.GetGraphic(20);

                // Update the QR image in the database
                await UpdateStudentQRImageAsync(studentId, qrCodeBase64);

                return qrCodeBase64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for student: {StudentId}", studentId);
                throw;
            }
        }

        private async Task UpdateStudentQRImageAsync(string studentId, string qrCodeBase64)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "UPDATE student SET QRImage = @QRImage WHERE StudentId = @StudentId";
            using var command = new MySqlCommand(query, connection);
            
            // Convert Base64 string to bytes for LONGBLOB storage (store as UTF8 bytes)
            var qrCodeBytes = System.Text.Encoding.UTF8.GetBytes(qrCodeBase64);
            _logger.LogInformation("Storing QR code for student {StudentId}: Base64 length {Base64Length}, Byte array length {ByteLength}", 
                studentId, qrCodeBase64.Length, qrCodeBytes.Length);
            
            command.Parameters.AddWithValue("@QRImage", qrCodeBytes);
            command.Parameters.AddWithValue("@StudentId", studentId);

            await command.ExecuteNonQueryAsync();
        }

        private async Task<string?> GetStudentQRImageAsync(string studentId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT QRImage FROM student WHERE StudentId = @StudentId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (reader.IsDBNull("QRImage"))
                {
                    _logger.LogInformation("No QR code found in database for student: {StudentId}", studentId);
                    return null;
                }

                // Get the data as byte array (LONGBLOB)
                var byteArray = (byte[])reader["QRImage"];
                _logger.LogInformation("QR code byte array length for student {StudentId}: {Length}", studentId, byteArray.Length);
                
                // Convert bytes to string (since it's stored as Base64 string in LONGBLOB)
                var base64String = System.Text.Encoding.UTF8.GetString(byteArray);
                _logger.LogInformation("Converted to string, length: {Length}, starts with: {Start}", base64String.Length, base64String.Substring(0, Math.Min(20, base64String.Length)));
                
                return base64String;
            }

            _logger.LogInformation("No student found with ID: {StudentId}", studentId);
            return null;
        }

        private async Task<StudentInfo?> GetStudentByIdAsync(string studentId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT StudentId, FullName, GradeLevel, Section, Strand, SchoolId FROM student WHERE StudentId = @StudentId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@StudentId", studentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new StudentInfo
                {
                    StudentId = reader.GetString("StudentId"),
                    FullName = reader.GetString("FullName"),
                    GradeLevel = reader.GetInt32("GradeLevel"),
                    Section = reader.GetString("Section"),
                    Strand = reader.IsDBNull("Strand") ? null : reader.GetString("Strand"),
                    SchoolId = reader.GetString("SchoolId")
                };
            }

            return null;
        }

        private async Task<SchoolInfo?> GetSchoolByIdAsync(string schoolId)
        {
            using var connection = new MySqlConnection(_dbConnection.GetConnection());
            await connection.OpenAsync();

            var query = "SELECT SchoolId, SchoolName, Region, Division, District, SchoolAddress FROM school WHERE SchoolId = @SchoolId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchoolId", schoolId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SchoolInfo
                {
                    SchoolId = reader.GetString("SchoolId"),
                    SchoolName = reader.GetString("SchoolName"),
                    Region = reader.GetString("Region"),
                    Division = reader.GetString("Division"),
                    District = reader.IsDBNull("District") ? null : reader.GetString("District"),
                    SchoolAddress = reader.IsDBNull("SchoolAddress") ? null : reader.GetString("SchoolAddress")
                };
            }

            return null;
        }
    }

    public class StudentRegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int GradeLevel { get; set; }
        public string Section { get; set; } = string.Empty;
        public string? Strand { get; set; }
        public string SchoolName { get; set; } = string.Empty;
    }

    public class StudentRegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public string? UserId { get; set; }
        public string? QRCodeData { get; set; }
    }

    public class StudentInfo
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int GradeLevel { get; set; }
        public string Section { get; set; } = string.Empty;
        public string? Strand { get; set; }
        public string SchoolId { get; set; } = string.Empty;
    }
}
