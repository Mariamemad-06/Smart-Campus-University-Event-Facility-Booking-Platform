using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs;

// Both create and update accept the same editable fields.
public sealed class FacilityWriteRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Type { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Required, RegularExpression("^(Available|Maintenance)$")]
    public string Status { get; set; } = "Available";

    [Required, StringLength(100)]
    public string Location { get; set; } = string.Empty;
}

public sealed record FacilityResponse(
    int Id, string Name, string Type, int Capacity, string Status, string Location)
{
    // Null for ordinary catalog queries; populated for availability searches.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FacilityAvailabilitySlot>? AvailableSlots { get; init; }
}

public sealed record FacilityAvailabilitySlot(DateTime StartTime, DateTime EndTime);

// Campus-local wall-clock times, matching the existing booking model.
public static class FacilityOperatingHours
{
    public static readonly TimeOnly Opening = new(8, 0);
    public static readonly TimeOnly Closing = new(20, 0);

    public static bool Contains(DateTime start, DateTime end) =>
        start < end && start.Date == end.Date &&
        TimeOnly.FromDateTime(start) >= Opening &&
        TimeOnly.FromDateTime(end) <= Closing;
}

public sealed class FacilitySearchRequest : IValidatableObject
{
    [StringLength(100)] public string? Keyword { get; set; }
    [StringLength(100)] public string? Type { get; set; }
    [StringLength(100)] public string? Location { get; set; }
    [Range(1, int.MaxValue)] public int? Capacity { get; set; }
    public DateOnly? Date { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date.HasValue && (StartTime.HasValue || EndTime.HasValue))
            yield return new ValidationResult("Use Date or StartTime/EndTime, not both.",
                new[] { nameof(Date), nameof(StartTime), nameof(EndTime) });
        if (StartTime.HasValue != EndTime.HasValue)
            yield return new ValidationResult("Both StartTime and EndTime are required for a time window.",
                new[] { nameof(StartTime), nameof(EndTime) });
        if (StartTime is DateTime start && EndTime is DateTime end)
        {
            if (start >= end)
                yield return new ValidationResult("EndTime must be after StartTime.", new[] { nameof(EndTime) });
            else if (!FacilityOperatingHours.Contains(start, end))
                yield return new ValidationResult("A requested time window must be on one day between 08:00 and 20:00 (campus local time).",
                    new[] { nameof(StartTime), nameof(EndTime) });
        }
    }
}
