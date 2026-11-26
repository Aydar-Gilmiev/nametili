using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IExpensesService
{
    Task<IActionResult> GetAsync(int tripId, int userId);
    Task<IActionResult> CreateAsync(int tripId, ExpenseRequest request, int userId);
    Task<IActionResult> UpdateAsync(int tripId, int expenseId, ExpenseRequest request, int userId);
    Task<IActionResult> DeleteAsync(int tripId, int expenseId, int userId);
}
