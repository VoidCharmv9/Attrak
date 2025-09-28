namespace AttrackSharedClass.Models
{
    public class DailyAttendanceRecord
    {
        public DateTime Date { get; set; }
        public string TimeIn { get; set; } = "";
        public string Status { get; set; } = "";
        public string Remarks { get; set; } = "";
    }

    public class DailyAttendanceStatus
    {
        public string Status { get; set; } = "";
        public string? TimeIn { get; set; }
    }

    public class DailyTimeInRequest
    {
        public string StudentId { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan TimeIn { get; set; }
    }

    public class DailyTimeInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Status { get; set; } = "";
        public string TimeIn { get; set; } = "";
    }
}
