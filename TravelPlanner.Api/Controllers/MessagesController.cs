using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Controllers;

[ApiController, Authorize, RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessagesService _service;
    public MessagesController(IMessagesService service) { _service = service; }

    [HttpGet]
    public Task<IActionResult> Get(int tripId, [FromQuery] int? beforeId = null) => _service.Get(tripId, beforeId);
}
