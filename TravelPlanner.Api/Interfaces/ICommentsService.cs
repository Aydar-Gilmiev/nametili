using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface ICommentsService
{
    Task<IActionResult> Get(int tripId, int proposalId, int? beforeId);
    Task<IActionResult> Save(int tripId, int proposalId, int? id, CommentRequest request, int userId);
    Task<IActionResult> Delete(int tripId, int proposalId, int id, int userId);
}
