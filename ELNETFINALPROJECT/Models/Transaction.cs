using System;
using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // "TopUp", "Guest", "Registration"

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? StationInfo { get; set; }
    }
}
