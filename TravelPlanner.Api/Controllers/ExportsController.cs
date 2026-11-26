using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Authorize]
[RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/export")]
public class ExportsController : ControllerBase
{
    private readonly IExportsService _service;
    private readonly TripAccess _access;
    public ExportsController(IExportsService service, TripAccess access)
    {
        _service = service;
        _access = access;
    }

    [HttpGet("pdf")]
    public Task<IActionResult> Pdf(int tripId) => _service.PdfAsync(tripId, _access.UserId(User));

}
