using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class Lab
    {
        [Required]
        public string Room { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "Available"; // Available, Occupied, Maintenance
    }
}
