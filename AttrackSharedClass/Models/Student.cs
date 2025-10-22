using System.ComponentModel.DataAnnotations;

namespace AttrackSharedClass.Models
{
    public class Student
    {
        [Key]
        public string StudentId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [Range(7, 12)]
        public int GradeLevel { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Section { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Strand { get; set; }
        
        [Required]
        public string SchoolId { get; set; } = string.Empty;
        
        [Required]
        [Phone]
        [MaxLength(20)]
        public string ParentsNumber { get; set; } = string.Empty;
        
        public string? QRImage { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
}
