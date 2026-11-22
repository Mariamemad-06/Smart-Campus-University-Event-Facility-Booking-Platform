using System.ComponentModel.DataAnnotations;

namespace BookingMVC.Models
{
    public class CreateBookingViewModel
    {
        [Required(ErrorMessage = "Please enter your User ID.")]
        public string UserId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a facility.")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Please select a start time.")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end time.")]
        public DateTime? EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Number of people must be greater than zero.")]
        public int NumberOfPeople { get; set; }
    }
}
