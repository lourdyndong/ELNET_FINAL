using System.ComponentModel.DataAnnotations;

namespace ELNETFINALPROJECT.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;
    }
}
