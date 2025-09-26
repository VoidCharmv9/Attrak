using Microsoft.AspNetCore.Mvc;
using ServerAtrrak.Services;

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
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AttendanceResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _attendanceService.MarkAttendanceAsync(request);
                
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

        [HttpGet("today/{subjectId}")]
        public async Task<ActionResult<List<AttendanceRecord>>> GetTodayAttendance(string subjectId)
        {
            try
            {
                var attendance = await _attendanceService.GetTodayAttendanceAsync(subjectId);
                return Ok(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for subject {SubjectId}: {ErrorMessage}", 
                    subjectId, ex.Message);
                return StatusCode(500, new List<AttendanceRecord>());
            }
        }

    }
}
