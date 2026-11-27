using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingAPI.Controllers;

[ApiController]
[Route("api/facilities")]
[Authorize(Roles = "Student,Faculty,Admin")]
public sealed class FacilitiesController(IFacilityService service) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<List<FacilityResponse>>> Search([FromQuery] FacilitySearchRequest request, CancellationToken ct) =>
        Ok(await service.SearchAsync(request, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FacilityResponse>> GetById(int id, CancellationToken ct)
    {
        var facility = await service.GetByIdAsync(id, ct);
        return facility is null ? NotFound() : Ok(facility);
    }
}
