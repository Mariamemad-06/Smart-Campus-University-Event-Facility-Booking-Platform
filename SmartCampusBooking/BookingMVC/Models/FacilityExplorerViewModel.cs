using System.ComponentModel.DataAnnotations;

namespace BookingMVC.Models;

public sealed class FacilityExplorerViewModel
{
    [StringLength(100)] public string? Keyword { get; set; }
    [StringLength(100)] public string? Type { get; set; }
    [StringLength(100)] public string? Location { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public List<string> FacilityTypes { get; set; } = new();

    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public List<string> FacilityLocations { get; set; } = new();

    [Range(1, int.MaxValue)] public int? Capacity { get; set; }
    public string? Date { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public List<FacilityExplorerItem> Facilities { get; set; } = new();
    public string? Error { get; set; }
    public bool SearchCompleted { get; set; }
}

public sealed class FacilityExplorerItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<FacilityExplorerSlot>? AvailableSlots { get; set; }
}

public sealed class FacilityExplorerSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
