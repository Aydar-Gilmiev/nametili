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
[Route("api/trips/{tripId:int}/repayments")]
public class RepaymentsController : ControllerBase
{
    private readonly IRepaymentsService _service;
    private readonly TripAccess _access;

    public RepaymentsController(IRepaymentsService service, TripAccess access)
    {
        _service = service;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int tripId)
    {
        return await _service.GetAsync(tripId, _access.UserId(User));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int tripId, [FromBody] RepaymentRequest request)
    {
        return await _service.CreateAsync(tripId, request, _access.UserId(User));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int tripId, int id, [FromBody] RepaymentRequest request)
    {
        return await _service.UpdateAsync(tripId, id, request, _access.UserId(User));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int tripId, int id)
    {
        return await _service.DeleteAsync(tripId, id, _access.UserId(User));
    }
}
