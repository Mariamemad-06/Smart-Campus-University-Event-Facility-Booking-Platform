
namespace BookingAPI.Models
    {
        public class Facility
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public string Type { get; set; } = string.Empty;

            public int Capacity { get; set; }

            public string Status { get; set; } = "Available";

            public string Location { get; set; } = string.Empty;
        }
    }
