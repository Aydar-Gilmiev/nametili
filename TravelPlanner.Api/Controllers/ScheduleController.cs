using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/schedule")]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _service;
    private readonly TripAccess _access;

    public ScheduleController(IScheduleService service, TripAccess access)
    {
        _service = service;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int tripId, [FromQuery] DateOnly? date = null)
    {
        return await _service.GetAsync(tripId, date, _access.UserId(User));
    }

    [HttpPost]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Create(int tripId, [FromBody] ScheduleItemRequest request)
    {
        return await _service.CreateAsync(tripId, request, _access.UserId(User));
    }

    [HttpPut("{id:int}")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Update(int tripId, int id, [FromBody] ScheduleItemRequest request)
    {
        return await _service.UpdateAsync(tripId, id, request, _access.UserId(User));
    }

    [HttpDelete("{id:int}")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Delete(int tripId, int id)
    {
        return await _service.DeleteAsync(tripId, id, _access.UserId(User));
    }
}
