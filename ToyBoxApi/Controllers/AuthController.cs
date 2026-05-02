using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Auth;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Login with email and password.</summary>
    /// <remarks>POST /api/auth/login</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Register a new account (Step 1 of 2).</summary>
    /// <remarks>POST /api/auth/register</remarks>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Complete profile setup (Step 2 of 2). Requires JWT.</summary>
    /// <remarks>PUT /api/auth/profile-setup</remarks>
    [HttpPut("profile-setup")]
    [Authorize]
    public async Task<IActionResult> ProfileSetup([FromBody] ProfileSetupRequest request)
    {
        var userId = User.GetUserId();
        var user = await _authService.ProfileSetupAsync(userId, request);
        return Ok(new { user });
    }
}
