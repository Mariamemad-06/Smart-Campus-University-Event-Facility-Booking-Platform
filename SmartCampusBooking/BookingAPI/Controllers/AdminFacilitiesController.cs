using BookingAPI.DTOs;
using BookingAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingAPI.Controllers;

[ApiController]
[Route("api/admin/facilities")]
[Authorize(Roles = "Admin")]
public sealed class AdminFacilitiesController(IFacilityService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FacilityResponse>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FacilityResponse>> GetById(int id, CancellationToken ct)
    {
        var facility = await service.GetByIdAsync(id, ct);
        return facility is null ? NotFound() : Ok(facility);
    }

    [HttpPost]
    public async Task<ActionResult<FacilityResponse>> Create(FacilityWriteRequest request, CancellationToken ct)
    {
        var facility = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = facility.Id }, facility);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FacilityResponse>> Update(int id, FacilityWriteRequest request,
        CancellationToken ct, [FromQuery] bool confirmImpact = false)
    {
        try
        {
            var facility = await service.UpdateAsync(id, request, confirmImpact, ct);
            if (facility is null) return NotFound();
            return Ok(facility);
        }
        catch (FacilityPolicyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct,
        [FromQuery] bool confirmImpact = false)
    {
        try
        {
            return (await service.DeleteAsync(id, confirmImpact, ct)) switch
            {
                FacilityDeleteResult.Deleted => NoContent(),
                FacilityDeleteResult.NotFound => NotFound(),
                _ => StatusCode(500)
            };
        }
        catch (FacilityPolicyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
