using Microsoft.EntityFrameworkCore;
using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<SitInRequest> SitInRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
    }
}
