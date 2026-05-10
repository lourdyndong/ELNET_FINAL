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
        public DbSet<Station> Stations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Station-Account relationship
            modelBuilder.Entity<Station>()
                .HasOne(s => s.CurrentPlayerAccount)
                .WithMany()
                .HasForeignKey(s => s.CurrentPlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
