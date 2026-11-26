using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Interfaces;

public interface IAuthService
{
    Task<IActionResult> RegisterAsync(RegisterRequest request);
    Task<IActionResult> LoginAsync(LoginRequest request);
}
