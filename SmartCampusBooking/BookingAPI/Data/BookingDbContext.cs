using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BookingAPI.Data
{
    public class BookingDbContext : DbContext
    {
        public BookingDbContext(DbContextOptions<BookingDbContext> options)
            : base(options)
        {
        }

        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UniversityId)
                .IsUnique()
                .HasFilter("[UniversityId] IS NOT NULL");

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Notification>(e =>
            {
                e.HasIndex(n => new { n.UserId, n.IsRead });

                e.HasOne(n => n.User)
                 .WithMany()                       // no collection on User
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade); // deleting a user removes their notifications
            });
        }

    }
}