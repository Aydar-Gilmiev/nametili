using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Services;
using TravelPlanner.Api.UniversalMethod;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Controllers;

[ApiController, Authorize, RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/photos")]
public class PhotosController : ControllerBase
{
    private readonly IPhotosService _service;
    private readonly TripAccess _access;
    public PhotosController(IPhotosService service, TripAccess access) { _service = service; _access = access; }
    [HttpGet]
    public Task<IActionResult> Get(int tripId, [FromQuery] int? beforeId = null) => _service.Get(tripId, beforeId);
    [HttpGet("{id:int}/content")]
    public Task<IActionResult> Content(int tripId, int id)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "sandbox";
        return _service.Content(tripId, id);
    }
    [HttpPost, RequestSizeLimit(PhotoHelper.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = PhotoHelper.MaxBytes + 65536)]
    public Task<IActionResult> Upload(int tripId, IFormFile file, [FromForm] string? caption) => _service.Upload(tripId, file, caption, _access.UserId(User));
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int tripId, int id) => _service.Delete(tripId, id, _access.UserId(User));
}
