using System.ComponentModel.DataAnnotations;

namespace BookingAPI.Models
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "UserId is required.")]
        public string UserId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "FacilityId must be greater than zero.")]
        public int FacilityId { get; set; }
        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime? StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime? EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "NumberOfPeople must be greater than zero.")]
        public int NumberOfPeople { get; set; }

    }
}
