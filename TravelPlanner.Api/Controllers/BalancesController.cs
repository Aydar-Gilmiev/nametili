using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/balances")]
public class BalancesController : ControllerBase
{
    private readonly IBalancesService _balancesService;
    private readonly TripAccess _access;

    public BalancesController(IBalancesService balancesService, TripAccess access)
    {
        _balancesService = balancesService;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int tripId)
    {
        return await _balancesService.GetAsync(tripId, _access.UserId(User));
    }
}
