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
        }

    }
}