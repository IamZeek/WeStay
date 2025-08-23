using Microsoft.EntityFrameworkCore;
using WeStay.AuthService.Models;

namespace WeStay.AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ExternalLogin> ExternalLogins { get; set; }
        public DbSet<Verification> Verifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<ExternalLogin>().ToTable("ExternalLogins");
            modelBuilder.Entity<Verification>().ToTable("Verifications");

            // Configure relationships
            modelBuilder.Entity<ExternalLogin>()
                .HasOne(el => el.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(el => el.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure 1-to-1 relationship between User and Verification
            modelBuilder.Entity<Verification>()
                .HasOne(v => v.User)
                .WithOne(u => u.Verification)
                .HasForeignKey<Verification>(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for better performance
            modelBuilder.Entity<Verification>()
                .HasIndex(v => v.UserId)
                .IsUnique();

            modelBuilder.Entity<Verification>()
                .HasIndex(v => v.DocumentNumber);

            modelBuilder.Entity<Verification>()
                .HasIndex(v => v.Status);
        }
    }
}