using Microsoft.AspNetCore.Mvc;
using AttrackSharedClass.Models;
using ServerAtrrak.Services;
using MySql.Data.MySqlClient;
using ServerAtrrak.Data;
using System.Data;

namespace ServerAtrrak.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DailyAttendanceController : ControllerBase
    {
        private readonly Dbconnection _dbConnection;
        private readonly ILogger<DailyAttendanceController> _logger;

        public DailyAttendanceController(Dbconnection dbConnection, ILogger<DailyAttendanceController> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        [HttpGet("daily-status/{studentId}")]
        public async Task<ActionResult<DailyAttendanceStatus>> GetDailyStatus(string studentId, [FromQuery] DateTime date)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT TimeIn, Status, Remarks 
                    FROM daily_attendance 
                    WHERE StudentId = @StudentId AND Date = @Date";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@Date", date.Date);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var timeIn = reader.IsDBNull("TimeIn") ? null : reader.GetString("TimeIn");
                    var status = reader.GetString("Status");
                    
                    return Ok(new DailyAttendanceStatus
                    {
                        Status = status,
                        TimeIn = timeIn
                    });
                }

                return Ok(new DailyAttendanceStatus
                {
                    Status = "Not Marked",
                    TimeIn = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily status for student: {StudentId}", studentId);
                return StatusCode(500, "Error retrieving daily status");
            }
        }

        [HttpPost("daily-timein")]
        public async Task<ActionResult<DailyTimeInResponse>> DailyTimeIn([FromBody] DailyTimeInRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new DailyTimeInResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if already marked for today
                var checkQuery = "SELECT COUNT(*) FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                checkCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (existingCount > 0)
                {
                    return BadRequest(new DailyTimeInResponse
                    {
                        Success = false,
                        Message = "Attendance already marked for today"
                    });
                }

                // Determine status based on time
                var timeInDateTime = request.Date.Date.Add(request.TimeIn);
                var schoolStartTime = request.Date.Date.AddHours(7).AddMinutes(30); // 7:30 AM
                var isLate = timeInDateTime > schoolStartTime;
                var status = isLate ? "Late" : "Present";

                // Insert attendance record
                var insertQuery = @"
                    INSERT INTO daily_attendance (AttendanceId, StudentId, Date, TimeIn, Status, Remarks, CreatedAt)
                    VALUES (@AttendanceId, @StudentId, @Date, @TimeIn, @Status, @Remarks, @CreatedAt)";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@AttendanceId", Guid.NewGuid().ToString());
                insertCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                insertCommand.Parameters.AddWithValue("@Date", request.Date.Date);
                insertCommand.Parameters.AddWithValue("@TimeIn", request.TimeIn.ToString(@"hh\:mm"));
                insertCommand.Parameters.AddWithValue("@Status", status);
                insertCommand.Parameters.AddWithValue("@Remarks", isLate ? "Late arrival" : "");
                insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                await insertCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Daily attendance marked for student: {StudentId}, Status: {Status}", request.StudentId, status);

                return Ok(new DailyTimeInResponse
                {
                    Success = true,
                    Message = "Attendance marked successfully",
                    Status = status,
                    TimeIn = request.TimeIn.ToString(@"hh\:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking daily attendance for student: {StudentId}", request.StudentId);
                return StatusCode(500, new DailyTimeInResponse
                {
                    Success = false,
                    Message = "An error occurred while marking attendance"
                });
            }
        }

        [HttpPost("daily-timeout")]
        public async Task<ActionResult<DailyTimeOutResponse>> DailyTimeOut([FromBody] DailyTimeOutRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new DailyTimeOutResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                // Check if Time In exists for today
                var checkQuery = "SELECT TimeIn, Status FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                checkCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                using var reader = await checkCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return BadRequest(new DailyTimeOutResponse
                    {
                        Success = false,
                        Message = "No Time In found for today. Please mark Time In first."
                    });
                }

                var timeIn = reader.GetString("TimeIn");
                var currentStatus = reader.GetString("Status");
                reader.Close();

                // Check if Time Out already exists
                var timeoutCheckQuery = "SELECT TimeOut FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date AND TimeOut IS NOT NULL";
                using var timeoutCheckCommand = new MySqlCommand(timeoutCheckQuery, connection);
                timeoutCheckCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                timeoutCheckCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                var existingTimeOut = await timeoutCheckCommand.ExecuteScalarAsync();
                if (existingTimeOut != null)
                {
                    return BadRequest(new DailyTimeOutResponse
                    {
                        Success = false,
                        Message = "Time Out already marked for today"
                    });
                }

                // Calculate remarks based on time ranges
                var timeInTime = TimeSpan.Parse(timeIn);
                var timeOutTime = request.TimeOut;
                var timeInHour = timeInTime.Hours;
                var timeOutHour = timeOutTime.Hours;
                
                string remarks;
                var sevenThirtyOne = new TimeSpan(7, 31, 0); // 7:31 AM
                var isLate = timeInTime >= sevenThirtyOne;
                
                // Check if it's a whole day (7:30 AM - 4:30 PM range)
                if (timeInHour <= 7 && timeOutHour >= 16) // 7:30 AM to 4:30 PM
                {
                    remarks = isLate ? "Late - Whole Day" : "Whole Day";
                }
                else
                {
                    // All other combinations are Half Day
                    remarks = isLate ? "Late - Half Day" : "Half Day";
                }

                // Update the record with Time Out
                var updateQuery = @"
                    UPDATE daily_attendance 
                    SET TimeOut = @TimeOut, 
                        Remarks = @Remarks,
                        UpdatedAt = @UpdatedAt
                    WHERE StudentId = @StudentId AND Date = @Date";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@TimeOut", request.TimeOut.ToString(@"hh\:mm"));
                updateCommand.Parameters.AddWithValue("@Remarks", remarks);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                updateCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                updateCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                await updateCommand.ExecuteNonQueryAsync();

                _logger.LogInformation("Daily Time Out marked for student: {StudentId}, Remarks: {Remarks}", request.StudentId, remarks);

                return Ok(new DailyTimeOutResponse
                {
                    Success = true,
                    Message = "Time Out marked successfully",
                    Remarks = remarks,
                    TimeOut = request.TimeOut.ToString(@"hh\:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking daily Time Out for student: {StudentId}", request.StudentId);
                return StatusCode(500, new DailyTimeOutResponse
                {
                    Success = false,
                    Message = "An error occurred while marking Time Out"
                });
            }
        }

        [HttpGet("today/{teacherId}")]
        public async Task<ActionResult<List<DailyAttendanceRecord>>> GetTodayAttendance(string teacherId)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT da.StudentId, s.FullName, da.Date, da.TimeIn, da.TimeOut, da.Status, da.Remarks
                    FROM daily_attendance da
                    INNER JOIN student s ON da.StudentId = s.StudentId
                    WHERE da.Date = @Date
                    ORDER BY da.Date DESC, da.TimeIn DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Date", DateTime.Today);

                using var reader = await command.ExecuteReaderAsync();
                var records = new List<DailyAttendanceRecord>();

                while (await reader.ReadAsync())
                {
                    var studentId = reader.GetString(0);
                    var studentName = reader.GetString(1);
                    var attendanceDate = reader.GetDateTime(2);
                    var timeIn = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    var timeOut = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    var status = reader.GetString(5);
                    var remarks = reader.IsDBNull(6) ? "" : reader.GetString(6);

                    records.Add(new DailyAttendanceRecord
                    {
                        StudentId = studentId,
                        StudentName = studentName,
                        Date = attendanceDate,
                        TimeIn = timeIn,
                        TimeOut = timeOut,
                        Status = status,
                        Remarks = remarks
                    });
                }

                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for teacher: {TeacherId}", teacherId);
                return StatusCode(500, new List<DailyAttendanceRecord>());
            }
        }

        [HttpGet("daily-history/{studentId}")]
        public async Task<ActionResult<List<DailyAttendanceRecord>>> GetDailyHistory(string studentId, [FromQuery] int days = 30)
        {
            try
            {
                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var query = @"
                    SELECT Date, TimeIn, TimeOut, Status, Remarks 
                    FROM daily_attendance 
                    WHERE StudentId = @StudentId 
                    AND Date >= @StartDate
                    ORDER BY Date DESC 
                    LIMIT @Days";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@StartDate", DateTime.Today.AddDays(-days));
                command.Parameters.AddWithValue("@Days", days);

                using var reader = await command.ExecuteReaderAsync();
                var records = new List<DailyAttendanceRecord>();

                while (await reader.ReadAsync())
                {
                    records.Add(new DailyAttendanceRecord
                    {
                        Date = reader.GetDateTime("Date"),
                        TimeIn = reader.IsDBNull("TimeIn") ? "" : reader.GetString("TimeIn"),
                        TimeOut = reader.IsDBNull("TimeOut") ? "" : reader.GetString("TimeOut"),
                        Status = reader.GetString("Status"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks")
                    });
                }

                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily history for student: {StudentId}", studentId);
                return StatusCode(500, new List<DailyAttendanceRecord>());
            }
        }
    }
}
