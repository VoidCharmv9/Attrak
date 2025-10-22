using AttrackSharedClass.Models;

namespace Attrak.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceResponse> MarkAttendanceAsync(AttendanceRequest request);
        Task<List<AttendanceRecord>> GetTodayAttendanceAsync(string teacherId);
        Task<bool> ValidateStudentEnrollmentAsync(string studentId, string teacherId, string schoolId);
        Task<Student?> GetStudentByQRCodeAsync(string qrCode);
    }
}
