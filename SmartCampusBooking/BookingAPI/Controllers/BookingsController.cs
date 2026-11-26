
using BookingAPI.Data;
using BookingAPI.Extensions;
using BookingAPI.Models;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly BookingDbContext _context;
        private readonly INotificationService _notificationService;

        public BookingsController(BookingDbContext context,INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBookingAsync(CreateBookingRequest request)
        {
            //Get validated date values
            var startTime = request.StartTime!.Value;
            var endTime = request.EndTime!.Value;

           
            if (startTime >= endTime)
            {
                return BadRequest("End time must be after start time.");
            }

            //Find the requested facility
            var facility = _context.Facilities.Find(request.FacilityId);

            if (facility == null)
            {
                return NotFound("Facility not found.");
            }

           
            if (facility.Status == "Maintenance")
            {
                return BadRequest(
                    "This facility is currently under maintenance.");
            }

         
            if (request.NumberOfPeople > facility.Capacity)
            {
                return BadRequest(
                    $"This facility can accommodate only {facility.Capacity} people.");
            }

            //Check for overlapping bookings
            var hasConflict = _context.Bookings.Any(b =>
                b.FacilityId == request.FacilityId &&
                b.Status == "Confirmed" &&
                startTime < b.EndTime &&
                endTime > b.StartTime);

            if (hasConflict)
            {
                return Conflict(
                    "This facility is already booked during this time.");
            }

            // Create a new booking
            var booking = new Booking
            {
                UserId = request.UserId,
                FacilityId = request.FacilityId,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Confirmed"
            };

            //Save booking to database
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            // Create automatic booking confirmation notification
            await _notificationService.CreateAsync(
                booking.UserId,
                "Booking Confirmed",
                $"Your booking for {facility.Name} has been confirmed successfully.",
                NotificationTypes.BookingConfirmed,
                booking.Id
            );

            return Ok(new
            {
                Message = "Booking created successfully!",
                BookingId = booking.Id,
                FacilityName = facility.Name,
                booking.StartTime,
                booking.EndTime
            });
        }

        [HttpGet("my-bookings")]
        [Authorize]
        public IActionResult GetMyBookings()
        {
            var userId = User.GetUserId();

            var bookings = _context.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => new
                {
                    b.Id,
                    b.FacilityId,
                    FacilityName = b.Facility.Name,
                    b.StartTime,
                    b.EndTime,
                    b.Status
                })
                .ToList();

            return Ok(bookings);
        }

        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.GetUserId();

            var booking = await _context.Bookings
                .Include(b => b.Facility)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            if (booking.Status == "Cancelled")
            {
                return BadRequest("Booking is already cancelled.");
            }

            booking.Status = "Cancelled";

            await _context.SaveChangesAsync();

            // Create automatic notification
            await _notificationService.CreateAsync(
                userId,
                "Booking Cancelled",
                $"Your booking for {booking.Facility?.Name} has been cancelled successfully.",
                NotificationTypes.BookingCancelled,
                booking.Id
            );

            return Ok(new
            {
                Message = "Booking cancelled successfully.",
                BookingId = booking.Id
            });
        }
    }
}
