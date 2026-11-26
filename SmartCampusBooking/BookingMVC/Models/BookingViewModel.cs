namespace BookingMVC.Models
{
    public class BookingViewModel
    {
        public int Id { get; set; }

        public int FacilityId { get; set; }

        public string FacilityName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
