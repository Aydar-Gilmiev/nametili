using Microsoft.AspNetCore.Mvc;

namespace TravelPlanner.Api.Interfaces;

public interface IExportsService
{
    Task<IActionResult> PdfAsync(int tripId, int userId);
}
