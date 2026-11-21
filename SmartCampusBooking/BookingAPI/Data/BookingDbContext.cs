//I made this class to act as bridge between  C# models and  actual database.

using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

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
    }
}
