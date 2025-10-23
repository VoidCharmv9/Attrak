using Microsoft.AspNetCore.Mvc;
using AttrackSharedClass.Models;
using ServerAtrrak.Services;
using ServerAtrrak.Models;
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

                // Check if already marked for today - get ALL records for this student/date
                var checkQuery = "SELECT AttendanceId, TimeIn, TimeOut FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date ORDER BY CreatedAt DESC";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                checkCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                using var reader = await checkCommand.ExecuteReaderAsync();
                var existingId = "";
                var existingTimeIn = "";
                var existingTimeOut = "";
                var hasMultipleRecords = false;
                
                if (await reader.ReadAsync())
                {
                    existingId = reader.GetString("AttendanceId");
                    existingTimeIn = reader.IsDBNull("TimeIn") ? "" : reader.GetString("TimeIn");
                    existingTimeOut = reader.IsDBNull("TimeOut") ? "" : reader.GetString("TimeOut");
                    
                    // Check if there are multiple records (duplicates)
                    if (await reader.ReadAsync())
                    {
                        hasMultipleRecords = true;
                        _logger.LogWarning("Found duplicate records for student {StudentId} on {Date}. Will consolidate.", request.StudentId, request.Date.Date);
                    }
                }
                reader.Close();
                
                // If there are duplicates, delete all but the first one
                if (hasMultipleRecords)
                {
                    var deleteQuery = "DELETE FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date AND AttendanceId != @KeepId";
                    using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                    deleteCommand.Parameters.AddWithValue("@Date", request.Date.Date);
                    deleteCommand.Parameters.AddWithValue("@KeepId", existingId);
                    await deleteCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Removed duplicate records for student {StudentId}", request.StudentId);
                }

                // Determine status based on time
                var timeInDateTime = request.Date.Date.Add(request.TimeIn);
                var schoolStartTime = request.Date.Date.AddHours(7).AddMinutes(30); // 7:30 AM
                var isLate = timeInDateTime > schoolStartTime;
                var status = isLate ? "Late" : "Present";

                if (!string.IsNullOrEmpty(existingId))
                {
                    // Update existing record
                    var updateQuery = @"
                        UPDATE daily_attendance 
                        SET TimeIn = @TimeIn, 
                            Status = @Status,
                            Remarks = @Remarks,
                            UpdatedAt = @UpdatedAt
                        WHERE AttendanceId = @AttendanceId";

                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@TimeIn", request.TimeIn.ToString(@"hh\:mm"));
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@Remarks", isLate ? "Late arrival" : "");
                    updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    updateCommand.Parameters.AddWithValue("@AttendanceId", existingId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
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
                }

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
                _logger.LogInformation("TimeOut request received for student: {StudentId}, Date: {Date}, TimeOut: {TimeOut}", 
                    request.StudentId, request.Date, request.TimeOut);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for TimeOut request: {ModelState}", ModelState);
                    return BadRequest(new DailyTimeOutResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();
                _logger.LogInformation("Database connection opened successfully");

                // Check if Time In exists for today - get the LATEST record to avoid duplicates
                var checkQuery = "SELECT AttendanceId, TimeIn, Status, TimeOut FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date ORDER BY CreatedAt DESC LIMIT 1";
                _logger.LogInformation("Executing query: {Query} with StudentId: {StudentId}, Date: {Date}", 
                    checkQuery, request.StudentId, request.Date.Date);
                
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@StudentId", request.StudentId);
                checkCommand.Parameters.AddWithValue("@Date", request.Date.Date);

                using var reader = await checkCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning("No TimeIn record found for student: {StudentId} on date: {Date}", 
                        request.StudentId, request.Date.Date);
                    return BadRequest(new DailyTimeOutResponse
                    {
                        Success = false,
                        Message = "No Time In found for today. Please mark Time In first."
                    });
                }

                var attendanceId = reader.GetString("AttendanceId");
                var timeIn = reader.GetString("TimeIn");
                var currentStatus = reader.GetString("Status");
                var existingTimeOut = reader.IsDBNull("TimeOut") ? "" : reader.GetString("TimeOut");
                reader.Close();
                
                _logger.LogInformation("Found TimeIn record - AttendanceId: {AttendanceId}, TimeIn: {TimeIn}, Status: {Status}, ExistingTimeOut: {ExistingTimeOut}", 
                    attendanceId, timeIn, currentStatus, existingTimeOut);

                // Check if Time Out already exists for this specific record
                if (!string.IsNullOrEmpty(existingTimeOut))
                {
                    _logger.LogWarning("TimeOut already exists for student: {StudentId}, existing TimeOut: {ExistingTimeOut}", 
                        request.StudentId, existingTimeOut);
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

                // Update the record with Time Out using specific AttendanceId
                var updateQuery = @"
                    UPDATE daily_attendance 
                    SET TimeOut = @TimeOut, 
                        Remarks = @Remarks,
                        UpdatedAt = @UpdatedAt
                    WHERE AttendanceId = @AttendanceId";

                _logger.LogInformation("Executing update query: {Query} with TimeOut: {TimeOut}, Remarks: {Remarks}, AttendanceId: {AttendanceId}", 
                    updateQuery, request.TimeOut.ToString(@"hh\:mm"), remarks, attendanceId);

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@TimeOut", request.TimeOut.ToString(@"hh\:mm"));
                updateCommand.Parameters.AddWithValue("@Remarks", remarks);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                updateCommand.Parameters.AddWithValue("@AttendanceId", attendanceId);

                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                _logger.LogInformation("Update completed, rows affected: {RowsAffected}", rowsAffected);

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
                _logger.LogError(ex, "Error marking daily Time Out for student: {StudentId}, TimeOut: {TimeOut}, Exception: {Exception}", 
                    request.StudentId, request.TimeOut, ex.ToString());
                return StatusCode(500, new DailyTimeOutResponse
                {
                    Success = false,
                    Message = $"An error occurred while marking Time Out: {ex.Message}"
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

        [HttpPost("sync-offline-data")]
        public async Task<ActionResult<SyncOfflineDataResponse>> SyncOfflineData([FromBody] SyncOfflineDataRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new SyncOfflineDataResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                _logger.LogInformation("Syncing offline data for teacher: {TeacherId}, Records count: {Count}", 
                    request.TeacherId, request.AttendanceRecords.Count);

                using var connection = new MySqlConnection(_dbConnection.GetConnection());
                await connection.OpenAsync();

                var syncedCount = 0;
                var errors = new List<string>();

                foreach (var record in request.AttendanceRecords)
                {
                    try
                    {
                        // Check if record already exists
                        var checkQuery = "SELECT COUNT(*) FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date";
                        using var checkCommand = new MySqlCommand(checkQuery, connection);
                        checkCommand.Parameters.AddWithValue("@StudentId", record.StudentId);
                        checkCommand.Parameters.AddWithValue("@Date", record.Date.Date);

                        var existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (existingCount > 0)
                        {
                            // Check if we're trying to update with duplicate TimeIn or TimeOut
                            var checkExistingQuery = "SELECT TimeIn, TimeOut FROM daily_attendance WHERE StudentId = @StudentId AND Date = @Date";
                            using var checkCommand = new MySqlCommand(checkExistingQuery, connection);
                            checkCommand.Parameters.AddWithValue("@StudentId", record.StudentId);
                            checkCommand.Parameters.AddWithValue("@Date", record.Date.Date);
                            
                            using var reader = await checkCommand.ExecuteReaderAsync();
                            if (await reader.ReadAsync())
                            {
                                var existingTimeIn = reader.IsDBNull("TimeIn") ? null : reader.GetString("TimeIn");
                                var existingTimeOut = reader.IsDBNull("TimeOut") ? null : reader.GetString("TimeOut");
                                reader.Close();
                                
                                // Only update if we're not trying to overwrite existing values with the same values
                                bool shouldUpdate = true;
                                
                                if (!string.IsNullOrEmpty(record.TimeIn) && !string.IsNullOrEmpty(existingTimeIn))
                                {
                                    _logger.LogWarning("TimeIn already exists for student {StudentId} on {Date}. Skipping duplicate TimeIn.", record.StudentId, record.Date);
                                    shouldUpdate = false;
                                }
                                
                                if (!string.IsNullOrEmpty(record.TimeOut) && !string.IsNullOrEmpty(existingTimeOut))
                                {
                                    _logger.LogWarning("TimeOut already exists for student {StudentId} on {Date}. Skipping duplicate TimeOut.", record.StudentId, record.Date);
                                    shouldUpdate = false;
                                }
                                
                                if (shouldUpdate)
                                {
                                    // Update existing record
                                    var updateQuery = @"
                                        UPDATE daily_attendance 
                                        SET TimeIn = COALESCE(@TimeIn, TimeIn),
                                            TimeOut = COALESCE(@TimeOut, TimeOut),
                                            Status = @Status,
                                            Remarks = @Remarks,
                                            UpdatedAt = @UpdatedAt
                                        WHERE StudentId = @StudentId AND Date = @Date";

                                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                                    updateCommand.Parameters.AddWithValue("@StudentId", record.StudentId);
                                    updateCommand.Parameters.AddWithValue("@Date", record.Date.Date);
                                    updateCommand.Parameters.AddWithValue("@TimeIn", string.IsNullOrEmpty(record.TimeIn) ? (object)DBNull.Value : record.TimeIn);
                                    updateCommand.Parameters.AddWithValue("@TimeOut", string.IsNullOrEmpty(record.TimeOut) ? (object)DBNull.Value : record.TimeOut);
                                    updateCommand.Parameters.AddWithValue("@Status", record.Status);
                                    updateCommand.Parameters.AddWithValue("@Remarks", record.Remarks ?? "");
                                    updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                                    await updateCommand.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                reader.Close();
                            }
                        }
                        else
                        {
                            // Insert new record
                            var insertQuery = @"
                                INSERT INTO daily_attendance (AttendanceId, StudentId, Date, TimeIn, TimeOut, Status, Remarks, CreatedAt, UpdatedAt)
                                VALUES (@AttendanceId, @StudentId, @Date, @TimeIn, @TimeOut, @Status, @Remarks, @CreatedAt, @UpdatedAt)";

                            using var insertCommand = new MySqlCommand(insertQuery, connection);
                            insertCommand.Parameters.AddWithValue("@AttendanceId", Guid.NewGuid().ToString());
                            insertCommand.Parameters.AddWithValue("@StudentId", record.StudentId);
                            insertCommand.Parameters.AddWithValue("@Date", record.Date.Date);
                            insertCommand.Parameters.AddWithValue("@TimeIn", string.IsNullOrEmpty(record.TimeIn) ? (object)DBNull.Value : record.TimeIn);
                            insertCommand.Parameters.AddWithValue("@TimeOut", string.IsNullOrEmpty(record.TimeOut) ? (object)DBNull.Value : record.TimeOut);
                            insertCommand.Parameters.AddWithValue("@Status", record.Status);
                            insertCommand.Parameters.AddWithValue("@Remarks", record.Remarks ?? "");
                            insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                            insertCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        syncedCount++;
                        _logger.LogInformation("Synced offline record for student: {StudentId}, Date: {Date}", 
                            record.StudentId, record.Date);
                    }
                    catch (Exception ex)
                    {
                        var error = $"Error syncing record for student {record.StudentId}: {ex.Message}";
                        errors.Add(error);
                        _logger.LogError(ex, "Error syncing offline record for student: {StudentId}", record.StudentId);
                    }
                }

                _logger.LogInformation("Offline sync completed. Synced: {SyncedCount}, Errors: {ErrorCount}", 
                    syncedCount, errors.Count);

                return Ok(new SyncOfflineDataResponse
                {
                    Success = true,
                    Message = $"Successfully synced {syncedCount} records",
                    SyncedCount = syncedCount,
                    ErrorCount = errors.Count,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing offline data for teacher: {TeacherId}", request.TeacherId);
                return StatusCode(500, new SyncOfflineDataResponse
                {
                    Success = false,
                    Message = "An error occurred while syncing offline data"
                });
            }
        }
    }
}
