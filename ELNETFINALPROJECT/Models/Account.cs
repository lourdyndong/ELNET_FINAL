namespace ELNETFINALPROJECT.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
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
        public decimal SessionHourlyRate { get; set; } = 25m; // Default ₱25/hour
    }
}
