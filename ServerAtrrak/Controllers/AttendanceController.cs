using Microsoft.AspNetCore.Mvc;
using ServerAtrrak.Services;
using AttrackSharedClass.Models;

namespace ServerAtrrak.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(AttendanceService attendanceService, ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        [HttpPost("mark")]
        public async Task<ActionResult<AttendanceResponse>> MarkAttendance([FromBody] AttendanceRequest request)
        {
            try
            {
                _logger.LogInformation("Received attendance request - StudentId: {StudentId}, TeacherId: {TeacherId}, SchoolId: {SchoolId}, AttendanceType: {AttendanceType}, Timestamp: {Timestamp}", 
                    request.StudentId, request.TeacherId, request.SchoolId, request.AttendanceType, request.Timestamp);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid request data for student {StudentId}", request.StudentId);
                    return BadRequest(new AttendanceResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _attendanceService.MarkAttendanceAsync(request);
                
                _logger.LogInformation("Attendance service result - Success: {Success}, IsValid: {IsValid}, Message: {Message}", 
                    result.Success, result.IsValid, result.Message);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(500, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance for student {StudentId}: {ErrorMessage}", 
                    request.StudentId, ex.Message);
                
                return StatusCode(500, new AttendanceResponse
                {
                    Success = false,
                    Message = "An error occurred while marking attendance"
                });
            }
        }

        [HttpGet("today/{teacherId}")]
        public async Task<ActionResult<List<AttendanceRecord>>> GetTodayAttendance(string teacherId)
        {
            try
            {
                var attendance = await _attendanceService.GetTodayAttendanceAsync(teacherId);
                return Ok(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for teacher {TeacherId}: {ErrorMessage}", 
                    teacherId, ex.Message);
                return StatusCode(500, new List<AttendanceRecord>());
            }
        }

    }
}
