using BookingAPI.DTOs;

namespace BookingAPI.Services;

public enum FacilityDeleteResult { Deleted, NotFound }

// A business-rule rejection, translated into HTTP 409 by the admin controller.
public sealed class FacilityPolicyException(string message) : Exception(message) { }

public interface IFacilityService
{
    Task<List<FacilityResponse>> GetAllAsync(CancellationToken ct = default);
    Task<FacilityResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FacilityResponse> CreateAsync(FacilityWriteRequest request, CancellationToken ct = default);
    Task<FacilityResponse?> UpdateAsync(int id, FacilityWriteRequest request, bool confirmImpact = false, CancellationToken ct = default);
    Task<FacilityDeleteResult> DeleteAsync(int id, bool confirmImpact = false, CancellationToken ct = default);
    Task<List<FacilityResponse>> SearchAsync(FacilitySearchRequest request, CancellationToken ct = default);
}
