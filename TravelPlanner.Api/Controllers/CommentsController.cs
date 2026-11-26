using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.CustomAttributes;
using TravelPlanner.Api.Requests;
using TravelPlanner.Api.Services;
using TravelPlanner.Api.Interfaces;

namespace TravelPlanner.Api.Controllers;

[ApiController, Authorize, RoleAuthorized("Organizer", "Participant")]
[Route("api/trips/{tripId:int}/proposals/{proposalId:int}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _service;
    private readonly TripAccess _access;
    public CommentsController(ICommentsService service, TripAccess access) { _service = service; _access = access; }
    [HttpGet]
    public Task<IActionResult> Get(int tripId, int proposalId, [FromQuery] int? beforeId = null) => _service.Get(tripId, proposalId, beforeId);
    [HttpPost]
    public Task<IActionResult> Create(int tripId, int proposalId, CommentRequest request) => _service.Save(tripId, proposalId, null, request, _access.UserId(User));
    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(int tripId, int proposalId, int id, CommentRequest request) => _service.Save(tripId, proposalId, id, request, _access.UserId(User));
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int tripId, int proposalId, int id) => _service.Delete(tripId, proposalId, id, _access.UserId(User));
}
