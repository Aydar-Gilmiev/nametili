using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IRepaymentsService
{
    Task<IActionResult> GetAsync(int tripId, int userId);
    Task<IActionResult> CreateAsync(int tripId, RepaymentRequest request, int userId);
    Task<IActionResult> UpdateAsync(int tripId, int repaymentId, RepaymentRequest request, int userId);
    Task<IActionResult> DeleteAsync(int tripId, int repaymentId, int userId);
}
