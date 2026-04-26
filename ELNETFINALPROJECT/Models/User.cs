using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class User
    {
        public int Id { get; set; }

        // For students this is the 8-digit student id; null for admin
        [MaxLength(8)]
        public string? StudentId { get; set; }

        // For admin users
        public string? Username { get; set; }

        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public string Role { get; set; } = "Student"; // Admin or Student
    }
}
