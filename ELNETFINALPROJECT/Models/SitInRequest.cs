using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class SitInRequest
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = null!; // 8-digit student id

        [Required]
        public string Room { get; set; } = null!; // e.g., 544

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    }
}
