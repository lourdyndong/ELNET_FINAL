using Microsoft.EntityFrameworkCore;
using ELNETFINALPROJECT.Models;

namespace ELNETFINALPROJECT.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
    }
}
