using System.Data;
using BookingAPI.Data;
using BookingAPI.DTOs;
using BookingAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Services;

public sealed class FacilityService(BookingDbContext context, INotificationService notificationService) : IFacilityService
{
    private static IQueryable<FacilityResponse> Project(IQueryable<Facility> query) =>
        query.Select(f => new FacilityResponse(f.Id, f.Name, f.Type, f.Capacity, f.Status, f.Location));

    private static FacilityResponse ToResponse(Facility f) =>
        new(f.Id, f.Name, f.Type, f.Capacity, f.Status, f.Location);

    private static void Apply(Facility facility, FacilityWriteRequest request)
    {
        facility.Name = request.Name.Trim();
        facility.Type = request.Type.Trim();
        facility.Capacity = request.Capacity;
        facility.Status = request.Status;
        facility.Location = request.Location.Trim();
    }

    public Task<List<FacilityResponse>> GetAllAsync(CancellationToken ct = default) =>
        Project(context.Facilities.AsNoTracking().OrderBy(f => f.Name).ThenBy(f => f.Id)).ToListAsync(ct);

    public Task<FacilityResponse?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Project(context.Facilities.AsNoTracking().Where(f => f.Id == id)).SingleOrDefaultAsync(ct);

    public async Task<FacilityResponse> CreateAsync(FacilityWriteRequest request, CancellationToken ct = default)
    {
        var facility = new Facility();
        Apply(facility, request);
        context.Facilities.Add(facility);
        await context.SaveChangesAsync(ct);
        return ToResponse(facility);
    }

    // Local campus clock: the existing project stores local DateTime booking values.
    private IQueryable<Booking> ActiveBookings(int facilityId, DateTime now) =>
        context.Bookings.Where(b => b.FacilityId == facilityId &&
            b.Status == "Confirmed" && b.EndTime > now);

    public async Task<FacilityResponse?> UpdateAsync(int id, FacilityWriteRequest request,
        bool confirmImpact = false, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var facility = await context.Facilities.SingleOrDefaultAsync(f => f.Id == id, ct);
        if (facility is null) return null;

        var active = await ActiveBookings(id, DateTime.Now).ToListAsync(ct);
        // Attendee count is not persisted in the existing Booking model.
        // Until the booking owner adds it, reject reductions with active reservations.
        if (request.Capacity < facility.Capacity && active.Count > 0)
            throw new FacilityPolicyException(
                "Capacity cannot be reduced while confirmed upcoming or ongoing bookings exist. The current booking model does not store attendee counts.");

        // Also resolves any active reservations on an already-maintenance facility.
        var closing = request.Status == "Maintenance" && active.Count > 0;
        if (closing && !confirmImpact)
            throw new FacilityPolicyException(
                $"Maintenance will cancel {active.Count} confirmed booking(s) and notify their users. Confirm the operation before proceeding.");

        var previousName = facility.Name;
        Apply(facility, request);
        if (closing)
        {
            foreach (var booking in active)
                booking.Status = "Cancelled";
            await context.SaveChangesAsync(ct);
            foreach (var booking in active)
            {
                ct.ThrowIfCancellationRequested();
                await notificationService.CreateAsync(booking.UserId,
                    "Booking Cancelled - Facility Maintenance",
                    $"Your reservation for {previousName} from {booking.StartTime:yyyy-MM-dd HH:mm} to {booking.EndTime:yyyy-MM-dd HH:mm} was cancelled because the facility is under maintenance.",
                    NotificationTypes.BookingCancelled, booking.Id);
            }
        }
        else
            await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResponse(facility);
    }

    public async Task<FacilityDeleteResult> DeleteAsync(int id, bool confirmImpact = false,
        CancellationToken ct = default)
    {
        // The existing database cascades facility deletion into all related bookings.
        // Keep notifications and deletion in a single SQL transaction.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var facility = await context.Facilities.SingleOrDefaultAsync(f => f.Id == id, ct);
        if (facility is null) return FacilityDeleteResult.NotFound;

        var active = await ActiveBookings(id, DateTime.Now).AsNoTracking().ToListAsync(ct);
        var total = await context.Bookings.CountAsync(b => b.FacilityId == id, ct);
        if (!confirmImpact)
            throw new FacilityPolicyException(
                $"Permanent deletion will remove the facility and all {total} associated booking record(s), including history. {active.Count} active booking(s) will be cancelled and their users notified. Confirm the operation before proceeding.");

        foreach (var booking in active)
        {
            ct.ThrowIfCancellationRequested();
            // No related booking ID: the record is about to be permanently removed.
            await notificationService.CreateAsync(booking.UserId,
                "Booking Cancelled - Facility Deleted",
                $"Your reservation for {facility.Name} from {booking.StartTime:yyyy-MM-dd HH:mm} to {booking.EndTime:yyyy-MM-dd HH:mm} was cancelled because the facility was permanently removed. Please choose another facility.",
                NotificationTypes.BookingCancelled);
        }
        context.Facilities.Remove(facility);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return FacilityDeleteResult.Deleted;
    }

    public async Task<List<FacilityResponse>> SearchAsync(FacilitySearchRequest request, CancellationToken ct = default)
    {
        IQueryable<Facility> query = context.Facilities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(f => f.Name.Contains(keyword) || f.Type.Contains(keyword) || f.Location.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim();
            query = query.Where(f => f.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim();
            query = query.Where(f => f.Location.Contains(location));
        }
        if (request.Capacity is int capacity)
            query = query.Where(f => f.Capacity >= capacity);

        if (request.Date is DateOnly date)
        {
            var dayStart = date.ToDateTime(FacilityOperatingHours.Opening);
            var dayEnd = date.ToDateTime(FacilityOperatingHours.Closing);
            query = query.Where(f => f.Status == "Available");

            var facilities = await Project(query.OrderBy(f => f.Name).ThenBy(f => f.Id)).ToListAsync(ct);
            if (facilities.Count == 0) return facilities;

            // Fetch overlapping confirmed bookings in one query (not one per facility).
            var bookings = await context.Bookings.AsNoTracking()
                .Where(b => b.Status == "Confirmed" && b.StartTime < dayEnd &&
                            b.EndTime > dayStart && b.StartTime < b.EndTime &&
                            query.Select(f => f.Id).Contains(b.FacilityId))
                .OrderBy(b => b.StartTime).ThenBy(b => b.EndTime)
                .Select(b => new { b.FacilityId, b.StartTime, b.EndTime })
                .ToListAsync(ct);
            var byFacility = bookings.ToLookup(b => b.FacilityId);
            var results = new List<FacilityResponse>();
            foreach (var facility in facilities)
            {
                var slots = new List<FacilityAvailabilitySlot>();
                var cursor = dayStart;
                foreach (var booking in byFacility[facility.Id])
                {
                    var busyStart = booking.StartTime < dayStart ? dayStart : booking.StartTime;
                    var busyEnd = booking.EndTime > dayEnd ? dayEnd : booking.EndTime;
                    if (busyStart > cursor)
                        slots.Add(new FacilityAvailabilitySlot(cursor, busyStart));
                    if (busyEnd > cursor) cursor = busyEnd;
                    if (cursor >= dayEnd) break;
                }
                if (cursor < dayEnd)
                    slots.Add(new FacilityAvailabilitySlot(cursor, dayEnd));

                // Hide facilities with no free time on the requested day.
                if (slots.Count > 0)
                    results.Add(facility with { AvailableSlots = slots });
            }
            return results;
        }

        // A specific time window must be within operating hours and free in its entirety.
        if (request.StartTime is DateTime from && request.EndTime is DateTime to)
        {
            if (!FacilityOperatingHours.Contains(from, to))
                throw new ArgumentException("The requested window is outside campus operating hours.");
            query = query.Where(f => f.Status == "Available" && !context.Bookings.Any(b =>
                b.FacilityId == f.Id && b.Status == "Confirmed" && from < b.EndTime && to > b.StartTime));
            var facilities = await Project(query.OrderBy(f => f.Name).ThenBy(f => f.Id)).ToListAsync(ct);
            return facilities.Select(f => f with
            {
                AvailableSlots = new[] { new FacilityAvailabilitySlot(from, to) }
            }).ToList();
        }
        return await Project(query.OrderBy(f => f.Name).ThenBy(f => f.Id)).ToListAsync(ct);
    }
}
