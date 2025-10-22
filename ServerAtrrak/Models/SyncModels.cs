using System.ComponentModel.DataAnnotations;

namespace ServerAtrrak.Models
{
    public class SyncOfflineDataRequest
    {
        [Required]
        public string TeacherId { get; set; } = string.Empty;
        
        [Required]
        public List<OfflineAttendanceRecord> AttendanceRecords { get; set; } = new();
    }

    public class OfflineAttendanceRecord
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;
        
        [Required]
        public DateTime Date { get; set; }
        
        public string? TimeIn { get; set; }
        
        public string? TimeOut { get; set; }
        
        [Required]
        public string Status { get; set; } = "Present";
        
        public string? Remarks { get; set; }
        
        public string? DeviceId { get; set; }
    }

    public class SyncOfflineDataResponse
    {
        public bool Success { get; set; }
        
        public string Message { get; set; } = string.Empty;
        
        public int SyncedCount { get; set; }
        
        public int ErrorCount { get; set; }
        
        public List<string> Errors { get; set; } = new();
    }
}
