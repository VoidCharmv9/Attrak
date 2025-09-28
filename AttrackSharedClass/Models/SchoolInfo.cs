namespace AttrackSharedClass.Models
{
    public class SchoolInfo
    {
        public string SchoolId { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string? District { get; set; }
        public string? SchoolAddress { get; set; }
    }
}
