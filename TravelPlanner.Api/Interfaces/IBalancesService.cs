using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Responses;

namespace TravelPlanner.Api.Interfaces;

public interface IBalancesService
{
    Task<IActionResult> GetAsync(int tripId, int userId);
    Task<List<BalanceResponse>> CalculateAsync(int tripId, int? excludedRepaymentId = null);
}
