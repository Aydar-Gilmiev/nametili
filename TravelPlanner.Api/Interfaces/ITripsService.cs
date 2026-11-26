using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface ITripsService
{
    Task<IActionResult> GetMineAsync(int userId);
    Task<IActionResult> CreateAsync(TripRequest request, int userId);
    Task<IActionResult> GetAsync(int tripId, int userId);
    Task<IActionResult> UpdateAsync(int tripId, TripRequest request, int userId);
    Task<IActionResult> DeleteAsync(int tripId, int userId);
    Task<IActionResult> InviteAsync(int tripId, InvitationRequest request, int userId);
    Task<IActionResult> ChangeRoleAsync(int tripId, int memberUserId, string role, int userId);
}
