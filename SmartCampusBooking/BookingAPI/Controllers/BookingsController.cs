
using BookingAPI.Data;
using BookingAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly BookingDbContext _context;

        public BookingsController(BookingDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreateBooking(CreateBookingRequest request)
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

            return Ok(new
            {
                Message = "Booking created successfully!",
                BookingId = booking.Id,
                FacilityName = facility.Name,
                booking.StartTime,
                booking.EndTime
            });
        }
    }
}
