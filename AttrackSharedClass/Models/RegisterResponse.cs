namespace AttrackSharedClass.Models
{
    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? TeacherId { get; set; }
    }
}
