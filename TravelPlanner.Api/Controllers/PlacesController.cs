using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Services;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Controllers;

[ApiController, Authorize, RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/places")]
public class PlacesController : ControllerBase
{
    private readonly IPlacesService _service;
    private readonly TripAccess _access;
    public PlacesController(IPlacesService service, TripAccess access) { _service = service; _access = access; }
    [HttpGet]
    public Task<IActionResult> Get(int tripId) => _service.Get(tripId);
    [HttpPost]
    public Task<IActionResult> Create(int tripId, PlaceRequest request) => _service.Save(tripId, null, request, _access.UserId(User));
    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int tripId, int id, PlaceRequest request) => _service.Save(tripId, id, request, _access.UserId(User));
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int tripId, int id) => _service.Delete(tripId, id, _access.UserId(User));
}
