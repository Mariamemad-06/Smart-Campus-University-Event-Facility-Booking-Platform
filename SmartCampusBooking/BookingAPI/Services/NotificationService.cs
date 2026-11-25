using BookingAPI.Data;
using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Services
{
    public static class NotificationTypes
    {
        public const string BookingConfirmed = "BookingConfirmed";
        public const string BookingCancelled = "BookingCancelled";
        public const string Reminder = "Reminder";
        public const string ScheduleChange = "ScheduleChange";
    }

    public interface INotificationService
    {
        Task CreateAsync(string userId, string title, string message, string type, int? bookingId = null);
        Task<List<Notification>> GetForUserAsync(string userId, bool unreadOnly = false);
        Task<int> UnreadCountAsync(string userId);
        Task<bool> MarkReadAsync(int id, string userId);
        Task MarkAllReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly BookingDbContext _db;

        public NotificationService(BookingDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(string userId, string title, string message, string type, int? bookingId = null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedBookingId = bookingId
            });
            await _db.SaveChangesAsync();
        }

        public Task<List<Notification>> GetForUserAsync(string userId, bool unreadOnly = false) =>
            _db.Notifications
                .Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

        public Task<int> UnreadCountAsync(string userId) =>
            _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task<bool> MarkReadAsync(int id, string userId)
        {
            var n = await _db.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n == null) return false;

            n.IsRead = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public Task MarkAllReadAsync(string userId) =>
            _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
