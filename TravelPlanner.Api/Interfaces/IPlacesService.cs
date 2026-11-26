using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IPlacesService
{
    Task<IActionResult> Get(int tripId);
    Task<IActionResult> Save(int tripId, int? id, PlaceRequest request, int userId);
    Task<IActionResult> Delete(int tripId, int id, int userId);
}
