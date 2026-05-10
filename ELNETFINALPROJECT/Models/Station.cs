namespace ELNETFINALPROJECT.Models
{
    public class Station
    {
        public int Id { get; set; }
        public int StationNumber { get; set; } // PC 1, PC 2, etc.
        public string Status { get; set; } = "offline"; // offline, available, active, unavailable
        public string? CurrentUser { get; set; } // Username of current player
        public int? CurrentPlayerId { get; set; } // FK to Account
        public int TimeUsedMinutes { get; set; } = 0;
        public int TimePaidMinutes { get; set; } = 0;
        public bool IsPoweredOn { get; set; } = false;
        public bool IsUnavailable { get; set; } = false; // Hardware problems
        public DateTime? SessionStartTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public virtual Account? CurrentPlayerAccount { get; set; }
    }
}
