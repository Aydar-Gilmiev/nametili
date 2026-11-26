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
[Route("api/trips/{tripId:int}/proposals")]
public class ProposalsController : ControllerBase
{
    private readonly IProposalsService _proposalsService;
    private readonly TripAccess _access;

    public ProposalsController(IProposalsService proposalsService, TripAccess access)
    {
        _proposalsService = proposalsService;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int tripId)
    {
        return await _proposalsService.GetAsync(tripId, _access.UserId(User));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int tripId, [FromBody] ProposalRequest request)
    {
        return await _proposalsService.CreateAsync(tripId, request, _access.UserId(User));
    }

    [HttpPut("{id:int}/vote")]
    public async Task<IActionResult> Vote(int tripId, int id, [FromBody] VoteRequest request)
    {
        return await _proposalsService.VoteAsync(tripId, id, request, _access.UserId(User));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int tripId, int id, [FromBody] ProposalRequest request)
    {
        return await _proposalsService.UpdateAsync(tripId, id, request, _access.UserId(User));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int tripId, int id)
    {
        return await _proposalsService.DeleteAsync(tripId, id, _access.UserId(User));
    }

    [HttpPatch("{id:int}/status")]
    [RoleAuthorized("Organizer")]
    public async Task<IActionResult> SetStatus(int tripId, int id, [FromBody] ProposalStatusRequest request)
    {
        return await _proposalsService.SetStatusAsync(tripId, id, request, _access.UserId(User));
    }

    [HttpGet("budget")]
    public async Task<IActionResult> Budget(int tripId)
    {
        return await _proposalsService.BudgetAsync(tripId, _access.UserId(User));
    }
}
