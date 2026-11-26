using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IScheduleService
{
    Task<IActionResult> GetAsync(int tripId, DateOnly? date, int userId);
    Task<IActionResult> CreateAsync(int tripId, ScheduleItemRequest request, int userId);
    Task<IActionResult> UpdateAsync(int tripId, int itemId, ScheduleItemRequest request, int userId);
    Task<IActionResult> DeleteAsync(int tripId, int itemId, int userId);
}
