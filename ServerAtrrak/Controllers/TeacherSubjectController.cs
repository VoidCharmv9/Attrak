using Microsoft.AspNetCore.Mvc;
using ServerAtrrak.Services;
using AttrackSharedClass.Models;

namespace ServerAtrrak.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherSubjectController : ControllerBase
    {
        private readonly TeacherSubjectService _teacherSubjectService;
        private readonly ILogger<TeacherSubjectController> _logger;

        public TeacherSubjectController(TeacherSubjectService teacherSubjectService, ILogger<TeacherSubjectController> logger)
        {
            _teacherSubjectService = teacherSubjectService;
            _logger = logger;
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<ActionResult<List<TeacherSubjectAssignment>>> GetTeacherSubjects(string teacherId)
        {
            try
            {
                var subjects = await _teacherSubjectService.GetTeacherSubjectsAsync(teacherId);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher subjects for teacher {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
                return StatusCode(500, new List<TeacherSubjectAssignment>());
            }
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<TeacherSubjectAssignment>>> GetAvailableSubjects([FromQuery] int? gradeLevel, [FromQuery] string? strand, [FromQuery] string? searchTerm)
        {
            try
            {
                var filter = new SubjectFilter
                {
                    GradeLevel = gradeLevel,
                    Strand = strand,
                    SearchTerm = searchTerm
                };

                var subjects = await _teacherSubjectService.GetAvailableSubjectsAsync(filter);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available subjects: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<TeacherSubjectAssignment>());
            }
        }

        [HttpPost("assign")]
        public async Task<ActionResult<TeacherSubjectResponse>> AssignSubject([FromBody] TeacherSubjectRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var response = await _teacherSubjectService.AssignSubjectAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subject: {ErrorMessage}", ex.Message);
                return StatusCode(500, new TeacherSubjectResponse
                {
                    Success = false,
                    Message = "An error occurred while assigning subject"
                });
            }
        }

        [HttpPut("schedule/{teacherSubjectId}")]
        public async Task<ActionResult<TeacherSubjectResponse>> UpdateSchedule(string teacherSubjectId, [FromBody] UpdateScheduleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var response = await _teacherSubjectService.UpdateSubjectScheduleAsync(teacherSubjectId, request.ScheduleStart, request.ScheduleEnd);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule: {ErrorMessage}", ex.Message);
                return StatusCode(500, new TeacherSubjectResponse
                {
                    Success = false,
                    Message = "An error occurred while updating schedule"
                });
            }
        }

        [HttpDelete("{teacherSubjectId}")]
        public async Task<ActionResult<TeacherSubjectResponse>> RemoveSubject(string teacherSubjectId)
        {
            try
            {
                var response = await _teacherSubjectService.RemoveSubjectAsync(teacherSubjectId);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing subject: {ErrorMessage}", ex.Message);
                return StatusCode(500, new TeacherSubjectResponse
                {
                    Success = false,
                    Message = "An error occurred while removing subject"
                });
            }
        }

        [HttpGet("grades")]
        public async Task<ActionResult<List<int>>> GetAvailableGrades()
        {
            try
            {
                var grades = await _teacherSubjectService.GetAvailableGradesAsync();
                return Ok(grades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available grades: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<int>());
            }
        }

        [HttpGet("strands")]
        public async Task<ActionResult<List<string>>> GetAvailableStrands()
        {
            try
            {
                var strands = await _teacherSubjectService.GetAvailableStrandsAsync();
                return Ok(strands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available strands: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<string>());
            }
        }

        [HttpGet("teachers")]
        public async Task<ActionResult<List<TeacherInfo>>> GetTeachers()
        {
            try
            {
                var teachers = await _teacherSubjectService.GetTeachersAsync();
                return Ok(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teachers: {ErrorMessage}", ex.Message);
                return StatusCode(500, new List<TeacherInfo>());
            }
        }

        [HttpGet("teacher-info/{teacherId}")]
        public async Task<ActionResult<TeacherInfo>> GetTeacherById(string teacherId)
        {
            try
            {
                var teacher = await _teacherSubjectService.GetTeacherByIdAsync(teacherId);
                if (teacher == null)
                {
                    return NotFound();
                }
                return Ok(teacher);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting teacher by ID {TeacherId}: {ErrorMessage}", teacherId, ex.Message);
                return StatusCode(500);
            }
        }

        [HttpPost("add-subject")]
        public async Task<ActionResult<TeacherSubjectResponse>> AddSubject([FromBody] NewSubjectRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TeacherSubjectResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var response = await _teacherSubjectService.AddSubjectAsync(request);
                
                if (response.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subject: {ErrorMessage}", ex.Message);
                return StatusCode(500, new TeacherSubjectResponse
                {
                    Success = false,
                    Message = "An error occurred while adding subject"
                });
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<List<StudentSubjectInfo>>> GetStudentSubjects(string studentId)
        {
            try
            {
                var subjects = await _teacherSubjectService.GetStudentSubjectsAsync(studentId);
                return Ok(subjects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student subjects for student {StudentId}: {ErrorMessage}", studentId, ex.Message);
                return StatusCode(500, new List<StudentSubjectInfo>());
            }
        }

        [HttpGet("sections/{subjectId}")]
        public async Task<ActionResult<List<SubjectSectionInfo>>> GetSubjectSections(string subjectId)
        {
            try
            {
                var sections = await _teacherSubjectService.GetSubjectSectionsAsync(subjectId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sections for subject {SubjectId}: {ErrorMessage}", subjectId, ex.Message);
                return StatusCode(500, new List<SubjectSectionInfo>());
            }
        }

        [HttpGet("available-sections/{subjectId}/{schoolName}")]
        public async Task<ActionResult<List<SubjectSectionInfo>>> GetAvailableSectionsForAssignment(string subjectId, string schoolName)
        {
            try
            {
                var sections = await _teacherSubjectService.GetAvailableSectionsForAssignmentAsync(subjectId, schoolName);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available sections for subject {SubjectId} and school {SchoolName}: {ErrorMessage}", subjectId, schoolName, ex.Message);
                return StatusCode(500, new List<SubjectSectionInfo>());
            }
        }

        [HttpPost("fix-database")]
        public async Task<ActionResult<string>> FixDatabase()
        {
            try
            {
                var result = await _teacherSubjectService.FixDatabaseAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing database: {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Error fixing database: {ex.Message}");
            }
        }

    }

    public class UpdateScheduleRequest
    {
        public TimeSpan ScheduleStart { get; set; }
        public TimeSpan ScheduleEnd { get; set; }
    }
}
