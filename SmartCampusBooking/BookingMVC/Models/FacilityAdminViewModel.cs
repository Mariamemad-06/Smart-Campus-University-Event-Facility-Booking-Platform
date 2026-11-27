using System.ComponentModel.DataAnnotations;

namespace BookingMVC.Models;

public sealed class FacilityAdminViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(100)]
    public string Type { get; set; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 1;
    [Required, RegularExpression("^(Available|Maintenance)$")]
    public string Status { get; set; } = "Available";
    [Required, StringLength(100)]
    public string Location { get; set; } = string.Empty;
}
