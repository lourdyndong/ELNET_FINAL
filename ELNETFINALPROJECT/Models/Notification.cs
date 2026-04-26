using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Message { get; set; } = null!;

        public bool IsRead { get; set; } = false;
    }
}
