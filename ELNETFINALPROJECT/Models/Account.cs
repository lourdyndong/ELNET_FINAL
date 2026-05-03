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
        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    }
}
