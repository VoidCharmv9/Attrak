using Microsoft.AspNetCore.Mvc;
using ServerAtrrak.Services;
using AttrackSharedClass.Models;

namespace ServerAtrrak.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QRValidationController : ControllerBase
    {
        private readonly QRValidationService _qrValidationService;
        private readonly ILogger<QRValidationController> _logger;

        public QRValidationController(QRValidationService qrValidationService, ILogger<QRValidationController> logger)
        {
            _qrValidationService = qrValidationService;
            _logger = logger;
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ServerQRValidationResult>> ValidateQRCode([FromBody] QRValidationRequest request)
        {
            try
            {
                _logger.LogInformation("QR validation request for teacher {TeacherId}", request.TeacherId);

                if (string.IsNullOrEmpty(request.QRCodeData))
                {
                    return BadRequest(new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = "QR code data is required",
                        ErrorType = ServerQRValidationErrorType.InvalidFormat
                    });
                }

                if (string.IsNullOrEmpty(request.TeacherId))
                {
                    return BadRequest(new ServerQRValidationResult
                    {
                        IsValid = false,
                        Message = "Teacher ID is required",
                        ErrorType = ServerQRValidationErrorType.TeacherNotFound
                    });
                }

                var result = await _qrValidationService.ValidateQRCodeAsync(request.QRCodeData, request.TeacherId);
                
                _logger.LogInformation("QR validation result for teacher {TeacherId}: {IsValid} - {Message}", 
                    request.TeacherId, result.IsValid, result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR code for teacher {TeacherId}: {ErrorMessage}", 
                    request.TeacherId, ex.Message);
                
                return StatusCode(500, new ServerQRValidationResult
                {
                    IsValid = false,
                    Message = "Internal server error during QR validation",
                    ErrorType = ServerQRValidationErrorType.ValidationError
                });
            }
        }

        [HttpGet("teacher/{teacherId}")]
        public ActionResult<TeacherInfo> GetTeacherInfo(string teacherId)
        {
            try
            {
                _logger.LogInformation("Getting teacher info for {TeacherId}", teacherId);

                // Return a placeholder for now
                return Ok(new TeacherInfo
                {
                    TeacherId = teacherId,
                    FullName = "Sample Teacher",
                    Email = "teacher@school.com",
                    SchoolName = "Sample School",
                    SchoolId = "SCH001",
                    GradeLevel = 10,
                    Section = "MEWOA"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher info for {TeacherId}: {ErrorMessage}", 
                    teacherId, ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class QRValidationRequest
    {
        public string QRCodeData { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
    }
}
