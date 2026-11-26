using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IProposalsService
{
    Task<IActionResult> GetAsync(int tripId, int userId);
    Task<IActionResult> CreateAsync(int tripId, ProposalRequest request, int userId);
    Task<IActionResult> UpdateAsync(int tripId, int proposalId, ProposalRequest request, int userId);
    Task<IActionResult> DeleteAsync(int tripId, int proposalId, int userId);
    Task<IActionResult> VoteAsync(int tripId, int proposalId, VoteRequest request, int userId);
    Task<IActionResult> SetStatusAsync(int tripId, int proposalId, ProposalStatusRequest request, int userId);
    Task<IActionResult> BudgetAsync(int tripId, int userId);
}
