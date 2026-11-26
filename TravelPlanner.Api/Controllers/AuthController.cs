using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Interfaces;
using TravelPlanner.Api.Requests;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        return await _authService.RegisterAsync(request);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        return await _authService.LoginAsync(request);
    }
}
