using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[RoleAuthorized]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly ITripsService _tripsService;
    private readonly TripAccess _access;

    public TripsController(ITripsService tripsService, TripAccess access)
    {
        _tripsService = tripsService;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        return await _tripsService.GetMineAsync(_access.UserId(User));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TripRequest request)
    {
        return await _tripsService.CreateAsync(request, _access.UserId(User));
    }

    [HttpGet("{tripId:int}")]
    [RoleAuthorized("Organizer", "Participant")]
    public async Task<IActionResult> Get(int tripId)
    {
        return await _tripsService.GetAsync(tripId, _access.UserId(User));
    }

    [HttpPost("{tripId:int}/invitations")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Invite(int tripId, [FromBody] InvitationRequest request)
    {
        return await _tripsService.InviteAsync(tripId, request, _access.UserId(User));
    }

    [HttpPut("{tripId:int}")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Update(int tripId, [FromBody] TripRequest request)
    {
        return await _tripsService.UpdateAsync(tripId, request, _access.UserId(User));
    }

    [HttpDelete("{tripId:int}")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> Delete(int tripId)
    {
        return await _tripsService.DeleteAsync(tripId, _access.UserId(User));
    }

    [HttpPatch("{tripId:int}/members/{memberUserId:int}/role")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> ChangeRole(int tripId, int memberUserId, [FromBody] string role)
    {
        return await _tripsService.ChangeRoleAsync(tripId, memberUserId, role, _access.UserId(User));
    }
}
