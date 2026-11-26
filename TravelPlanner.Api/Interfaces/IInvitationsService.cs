using Microsoft.AspNetCore.Mvc;

namespace TravelPlanner.Api.Interfaces;

public interface IInvitationsService
{
    Task<IActionResult> GetMineAsync(string email);
    Task<IActionResult> AcceptAsync(int invitationId, string email, int userId);
}
