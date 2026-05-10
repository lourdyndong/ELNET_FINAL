using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(256, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Balance cannot be negative")]
        public decimal Balance { get; set; }

        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
        public byte[]? ProfilePicture { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsVerified { get; set; } = false;
        
        // Player activity tracking
        public int TotalSessions { get; set; } = 0;
        public int TotalPlaytimeMinutes { get; set; } = 0;
        
        // Current session (if active)
        public string? CurrentGame { get; set; }
        public string? CurrentStation { get; set; }
        public DateTime? SessionStartTime { get; set; }
        public decimal SessionHourlyRate { get; set; } = 20m; // Default ₱20/hour
    }
}
