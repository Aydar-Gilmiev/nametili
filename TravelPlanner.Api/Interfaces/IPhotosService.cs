using Microsoft.AspNetCore.Mvc;

namespace TravelPlanner.Api.Interfaces;

public interface IPhotosService
{
    Task<IActionResult> Get(int tripId, int? beforeId);
    Task<IActionResult> Content(int tripId, int id);
    Task<IActionResult> Upload(int tripId, IFormFile file, string? caption, int userId);
    Task<IActionResult> Delete(int tripId, int id, int userId);
}
