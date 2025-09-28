using System.ComponentModel.DataAnnotations;

namespace AttrackSharedClass.Models
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Region { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Division { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? District { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SchoolName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? SchoolAddress { get; set; }
        
        [Required]
        public int GradeLevel { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Section { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Strand { get; set; }
    }
}
