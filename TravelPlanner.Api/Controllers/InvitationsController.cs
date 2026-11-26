using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[RoleAuthorized]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationsService _invitationsService;
    private readonly TripAccess _access;

    public InvitationsController(IInvitationsService invitationsService, TripAccess access)
    {
        _invitationsService = invitationsService;
        _access = access;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        return await _invitationsService.GetMineAsync(email);
    }

    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        return await _invitationsService.AcceptAsync(id, email, _access.UserId(User));
    }
}
