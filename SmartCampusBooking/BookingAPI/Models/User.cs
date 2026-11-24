using System;

namespace BookingAPI.Models
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();

        public string FullName { get; set; } = string.Empty;

        public string? UniversityId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}